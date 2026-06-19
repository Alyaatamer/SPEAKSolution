using Microsoft.AspNetCore.Http;
using SPEAK.Abstraction.IServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using SPEAK.Domain.Models.Identity;
using System.Text;
using SPEAK.Shared.DTO_s;
using SPEAK.Shared.ErrorModels;
using SPEAK.Abstraction.IRepositories;
using SPEAK.Domain.Models;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace SPEAK.Service.Services
{
    public class VoiceProcessingService : IVoiceProcessingService
    {
        private readonly IAudioMerger _audioMerger;
        private readonly HttpClient _httpClient;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDiagnosticRepository _diagnosticRepository;
        private const string UploadFolderName = "voices";

        // AI Service URLs - loaded from appsettings.json
        private readonly string _segmentUrl;
        private readonly string _analyzeUrl;

        private readonly IAuthenticationServices _authServices;

        public VoiceProcessingService(
            IAudioMerger audioMerger,
            HttpClient httpClient,
            UserManager<ApplicationUser> userManager,
            IDiagnosticRepository diagnosticRepository,
            IAuthenticationServices authServices,
            IConfiguration configuration)
        {
            _audioMerger = audioMerger;
            _httpClient = httpClient;
            _userManager = userManager;
            _diagnosticRepository = diagnosticRepository;
            _authServices = authServices;

            // Load AI service URLs from appsettings.json
            _segmentUrl  = configuration["AI:SegmentUrl"]  ?? throw new InvalidOperationException("AI:SegmentUrl is not configured in appsettings.json");
            _analyzeUrl  = configuration["AI:AnalyzeUrl"]  ?? throw new InvalidOperationException("AI:AnalyzeUrl is not configured in appsettings.json");

            // Set timeout for long-running AI operations
            _httpClient.Timeout = TimeSpan.FromMinutes(10);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Step 1 – Merge local WAV files → call /segment → derive word count
        // ─────────────────────────────────────────────────────────────────────
        public async Task<(int WordCount, string MergedFilePath)> ProcessVoiceSessionAsync(
            string userId,
            List<IFormFile> files,
            string type,
            string webRootPath)
        {
            // 1. Setup user-isolated folder
            var uploadFolder = Path.Combine(webRootPath, UploadFolderName, userId);
            Directory.CreateDirectory(uploadFolder);

            // Determine merged WAV filename by task type
            string mergedFileName = type switch
            {
                "images"  => "merged_images.wav",
                "reading" => "merged_reading.wav",
                _         => "merged.wav"
            };
            var mergedFilePath = Path.Combine(uploadFolder, mergedFileName);

            // ZIP file to be saved for later /analyze call
            string zipFileName = type switch
            {
                "reading" => "reading_zip.zip",
                _         => "speaking_zip.zip"
            };
            var zipFilePath = Path.Combine(uploadFolder, zipFileName);

            // 2. Save newly uploaded recordings to disk
            var savedFiles = new List<string>();
            try
            {
                foreach (var file in files)
                {
                    var filePath = Path.Combine(uploadFolder, file.FileName);
                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);
                    await File.WriteAllBytesAsync(filePath, ms.ToArray());
                    savedFiles.Add(filePath);
                }

                // 3. Accumulate: prepend any existing merged WAV so recordings build up
                var filesToMerge = new List<string>();
                if (File.Exists(mergedFilePath))
                    filesToMerge.Add(mergedFilePath);
                filesToMerge.AddRange(savedFiles);

                // 4. Local merge with NAudio
                var tempMergedPath = Path.Combine(uploadFolder, $"temp_{Guid.NewGuid():N}.wav");
                await _audioMerger.MergeAudioFilesAsync(filesToMerge, tempMergedPath);

                if (File.Exists(mergedFilePath))
                    File.Delete(mergedFilePath);
                File.Move(tempMergedPath, mergedFilePath);

                // 5. Send merged WAV to /segment → get back a ZIP of word_X.wav files
                //    If the AI returned 400 (< 100 words), ZipBytes is null and WordCountFromError has the count.
                var (zipBytes, wordCountFromError) = await CallSegmentAsync(mergedFilePath);

                // 6. If AI said < 100 words, return that count — Flutter shows retry message
                if (zipBytes == null)
                    return (wordCountFromError ?? 0, mergedFilePath);

                // 7. Count word files inside the ZIP to determine word count
                int wordCount = CountWordsInZip(zipBytes);

                // 8. Always save the ZIP (even if < 100) so /analyze can pick it up later
                //    when the user retries and accumulates more recordings.
                await File.WriteAllBytesAsync(zipFilePath, zipBytes);

                return (wordCount, mergedFilePath);
            }
            finally
            {
                // 8. Delete the raw individual recordings; merged WAV and ZIP stay
                foreach (var f in savedFiles)
                    if (File.Exists(f)) File.Delete(f);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Step 2 – Send saved ZIP(s) to /analyze → return SSI JSON
        // ─────────────────────────────────────────────────────────────────────
        public async Task<string> CalculateSSIAsync(string userId, SSIDetectionRequestDto request, string webRootPath)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new Exception("User not found");

            var userDto = await _authServices.GetCurrentUserAsync(user.Email);
            int age = userDto.ChildAge ?? 0;

            var uploadFolder = Path.Combine(webRootPath, UploadFolderName, userId);

            // Locate speaking ZIP (general or images task)
            string speakingZipPath = Path.Combine(uploadFolder, "speaking_zip.zip");
            if (!File.Exists(speakingZipPath))
                throw new Exception("No speaking audio ZIP found. Please complete the recording step first.");

            // Locate reading ZIP (only when isReader = true)
            string? readingZipPath = null;
            if (request.IsReader)
            {
                string rp = Path.Combine(uploadFolder, "reading_zip.zip");
                if (File.Exists(rp)) readingZipPath = rp;
            }

            // Call /analyze and get SSI result
            string finalJson = await CallAnalyzeAsync(speakingZipPath, readingZipPath, age, request);

            // Persist to database
            try
            {
                using var doc = JsonDocument.Parse(finalJson);
                var root = doc.RootElement;

                string severity = "Unknown";
                string labelCountsParams = "{}";

                if (root.TryGetProperty("severity", out var severityEl))
                {
                    severity = severityEl.ValueKind == JsonValueKind.String
                        ? severityEl.GetString() ?? "Unknown"
                        : severityEl.TryGetProperty("level", out var lvl) ? lvl.GetString() ?? "Unknown" : "Unknown";
                }

                if (root.TryGetProperty("label_counts", out var lcEl))
                    labelCountsParams = lcEl.GetRawText();
                else if (root.TryGetProperty("summary", out var sumEl) &&
                         sumEl.TryGetProperty("label_counts", out var sumLc))
                    labelCountsParams = sumLc.GetRawText();

                var record = new DiagnosticRecord
                {
                    UserId       = userId,
                    Severity     = severity,
                    LabelCountsJson = labelCountsParams,
                    FullResultJson  = finalJson,
                    CreatedAt    = DateTime.UtcNow
                };
                await _diagnosticRepository.AddDiagnosticRecordAsync(record);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving diagnostic record: {ex.Message}");
            }

            return finalJson;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Cleanup – removes merged WAV *and* the saved ZIP for that task type
        // ─────────────────────────────────────────────────────────────────────
        public async Task CleanupMergedFileAsync(string userId, string type, string webRootPath)
        {
            try
            {
                var uploadFolder = Path.Combine(webRootPath, UploadFolderName, userId);

                string mergedFileName = type switch
                {
                    "images"  => "merged_images.wav",
                    "reading" => "merged_reading.wav",
                    _         => "merged.wav"
                };
                string zipFileName = type switch
                {
                    "reading" => "reading_zip.zip",
                    _         => "speaking_zip.zip"
                };

                string mergedPath = Path.Combine(uploadFolder, mergedFileName);
                string zipPath    = Path.Combine(uploadFolder, zipFileName);

                if (File.Exists(mergedPath)) File.Delete(mergedPath);
                if (File.Exists(zipPath))    File.Delete(zipPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CleanupMergedFileAsync: {ex.Message}");
            }
            await Task.CompletedTask;
        }

        public async Task<DiagnosticRecord?> GetLatestDiagnosisAsync(string userId)
        {
            return await _diagnosticRepository.GetLatestDiagnosticRecordAsync(userId);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Private Helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// POST merged WAV to /segment.
        /// The API returns a JSON-encoded base64 string (application/json → "UEsD...").
        /// We read the string, strip the surrounding JSON quotes, then base64-decode
        /// to get the real ZIP bytes.
        /// </summary>
        private async Task<(byte[]? ZipBytes, int? WordCountFromError)> CallSegmentAsync(string mergedWavPath)
        {
            using var formData = new MultipartFormDataContent();
            var fileBytes   = await File.ReadAllBytesAsync(mergedWavPath);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");
            formData.Add(fileContent, "file", Path.GetFileName(mergedWavPath));

            var response = await _httpClient.PostAsync(_segmentUrl, formData);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();

                // The AI may return 400 (word count < 100) either directly or
                // wrapped inside a 500 ("Pipeline failed: 400: {...}").
                // We check both status codes and use regex to extract word_count
                // from any position in the response body.
                bool isWordCountError =
                    response.StatusCode == System.Net.HttpStatusCode.BadRequest ||
                    response.StatusCode == System.Net.HttpStatusCode.InternalServerError;

                if (isWordCountError)
                {
                    // Strategy 1: detail is a JSON object → { "detail": { "word_count": N } }
                    try
                    {
                        using var doc = JsonDocument.Parse(errorBody);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("detail", out var detail) &&
                            detail.ValueKind == JsonValueKind.Object &&
                            detail.TryGetProperty("word_count", out var wc))
                        {
                            return (null, wc.GetInt32());
                        }
                    }
                    catch { /* not valid JSON or wrong shape */ }

                    // Strategy 2: word_count is embedded in a string (Python dict format)
                    // e.g. "Pipeline failed: 400: {'word_count': 5, ...}"
                    var regexMatch = System.Text.RegularExpressions.Regex.Match(
                        errorBody, @"['""]?word_count['""]?\s*:\s*(\d+)");
                    if (regexMatch.Success && int.TryParse(regexMatch.Groups[1].Value, out var wordCountFromRegex))
                    {
                        return (null, wordCountFromRegex);
                    }
                }

                throw new Exception($"Segment API failed ({response.StatusCode}): {errorBody}");
            }

            // /segment returns a binary ZIP (application/zip) via FileResponse.
            // Read raw bytes directly.
            var zipBytes = await response.Content.ReadAsByteArrayAsync();
            return (zipBytes, null);
        }

        /// <summary>
        /// Counts the number of .wav entries in a ZIP to determine word count.
        /// </summary>
        private static int CountWordsInZip(byte[] zipBytes)
        {
            using var ms      = new MemoryStream(zipBytes);
            using var archive = new ZipArchive(ms, ZipArchiveMode.Read);
            return archive.Entries.Count(e =>
                e.Name.EndsWith(".wav", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// POST speaking ZIP (and optionally reading ZIP) + physical params to /analyze.
        /// </summary>
        private async Task<string> CallAnalyzeAsync(
            string speakingZipPath,
            string? readingZipPath,
            int age,
            SSIDetectionRequestDto request)
        {
            using var formData = new MultipartFormDataContent();

            // Speaking ZIP (always required)
            var speakingBytes = await File.ReadAllBytesAsync(speakingZipPath);
            var speakingContent = new ByteArrayContent(speakingBytes);
            speakingContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/zip");
            formData.Add(speakingContent, "speaking_zip", "speaking_zip.zip");

            // Core parameters
            formData.Add(new StringContent(age.ToString()), "age");
            // can_read is a boolean field; send "true" / "false" (FastAPI accepts both)
            formData.Add(new StringContent(request.IsReader ? "true" : "false"), "can_read");

            // Reading ZIP (optional, only when can_read = true)
            if (request.IsReader && readingZipPath != null)
            {
                var readingBytes   = await File.ReadAllBytesAsync(readingZipPath);
                var readingContent = new ByteArrayContent(readingBytes);
                readingContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/zip");
                formData.Add(readingContent, "reading_zip", "reading_zip.zip");
            }

            // Physical concomitants
            formData.Add(new StringContent(request.DistractingSounds.ToString()), "distracting_sounds");
            formData.Add(new StringContent(request.FacialGrimaces.ToString()),    "facial_grimaces");
            formData.Add(new StringContent(request.HeadMovements.ToString()),     "head_movements");
            formData.Add(new StringContent(request.Extremities.ToString()),       "extremities");

            var response = await _httpClient.PostAsync(_analyzeUrl, formData);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                // Check for word-count error from the AI pipeline
                if (TryExtractWordCountError(error, out var wordCount, out var messageEn, out var messageAr))
                    throw new WordCountException(messageEn, messageAr, wordCount);

                throw new Exception($"Analyze API failed ({response.StatusCode}): {error}");
            }

            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Tries to parse word-count error details from a pipeline 400 response body.
        /// </summary>
        private static bool TryExtractWordCountError(
            string error,
            out int wordCount,
            out string messageEn,
            out string messageAr)
        {
            wordCount = 0;
            messageEn = string.Empty;
            messageAr = string.Empty;

            try
            {
                using var doc = JsonDocument.Parse(error);
                if (doc.RootElement.TryGetProperty("detail", out var detailProp))
                {
                    var detail = detailProp.GetString();
                    if (detail != null && detail.Contains("word_count"))
                    {
                        int firstBrace = detail.IndexOf('{');
                        int lastBrace  = detail.LastIndexOf('}');
                        if (firstBrace >= 0 && lastBrace > firstBrace)
                        {
                            var dictStr = detail.Substring(firstBrace, lastBrace - firstBrace + 1);
                            var jsonStr = dictStr.Replace('\'', '"');
                            using var innerDoc = JsonDocument.Parse(jsonStr);
                            var innerRoot = innerDoc.RootElement;
                            if (innerRoot.TryGetProperty("status", out var status) && status.GetString() == "error")
                            {
                                wordCount = innerRoot.TryGetProperty("word_count", out var wc) ? wc.GetInt32() : 0;
                                messageEn = innerRoot.TryGetProperty("message_en", out var me) ? me.GetString() ?? string.Empty : string.Empty;
                                messageAr = innerRoot.TryGetProperty("message_ar", out var ma) ? ma.GetString() ?? string.Empty : string.Empty;
                                return true;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Parsing failed – fall through
            }
            return false;
        }
    }
}
