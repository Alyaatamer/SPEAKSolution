using Microsoft.AspNetCore.Http;
using SPEAK.Abstraction.IServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using SPEAK.Domain.Models.Identity;
using System.Text;
using SPEAK.Shared.DTO_s;
using SPEAK.Abstraction.IRepositories;
using SPEAK.Domain.Models;

namespace SPEAK.Service.Services
{
    public class VoiceProcessingService : IVoiceProcessingService
    {
        private readonly IAudioMerger _audioMerger;
        private readonly HttpClient _httpClient;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDiagnosticRepository _diagnosticRepository;
        private const string UploadFolderName = "UploadedVoices";
        
        // Configuration/Constants - ideally injected via IConfiguration, but keeping consistent with Controller for now
        private const string NoiseCancellationUrl = "http://localhost:8000/enhance"; 
        private const string SegmentationUrl = "https://possessively-nonsidereal-rachal.ngrok-free.dev/segment";

        private readonly IAuthenticationServices _authServices;

        public VoiceProcessingService(IAudioMerger audioMerger, HttpClient httpClient, UserManager<ApplicationUser> userManager, IDiagnosticRepository diagnosticRepository, IAuthenticationServices authServices)
        {
            _audioMerger = audioMerger;
            _httpClient = httpClient;
            _userManager = userManager;
            _diagnosticRepository = diagnosticRepository;
            _authServices = authServices;
            // Set timeout for long running AI operations
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        public async Task<(int WordCount, string MergedFilePath)> ProcessVoiceSessionAsync(List<IFormFile> files, string type, string contentRootPath)
        {
             // 1. Setup Paths
            var uploadFolder = Path.Combine(contentRootPath, UploadFolderName);
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            // Determine filename based on type
            string mergedFileName = type switch
            {
                "images" => "merged_images.wav",
                "reading" => "merged_reading.wav",
                _ => "merged.wav"
            };
            var mergedFilePath = Path.Combine(uploadFolder, mergedFileName);

            // 2. Save New Files
            var savedFiles = new List<string>();
            try 
            {
                foreach (var file in files)
                {
                    var filePath = Path.Combine(uploadFolder, file.FileName);
                    using var memoryStream = new MemoryStream();
                    await file.CopyToAsync(memoryStream);
                    await System.IO.File.WriteAllBytesAsync(filePath, memoryStream.ToArray());
                    savedFiles.Add(filePath);
                }

                // 3. Accumulation & Safe Merge
                var filesToMerge = new List<string>();
                if (System.IO.File.Exists(mergedFilePath))
                {
                    filesToMerge.Add(mergedFilePath);
                }
                filesToMerge.AddRange(savedFiles);

                // Merge to temp file
                var tempMergedPath = Path.Combine(uploadFolder, $"temp_{Guid.NewGuid()}.wav");
                await _audioMerger.MergeAudioFilesAsync(filesToMerge, tempMergedPath);

                // Replace existing merged file
                if (System.IO.File.Exists(mergedFilePath))
                {
                    System.IO.File.Delete(mergedFilePath);
                }
                System.IO.File.Move(tempMergedPath, mergedFilePath);

                // 4. Processing Pipeline (Noise Cancellation -> Segmentation)
                
                // Step 4a: Noise Cancellation
                byte[] cleanedFileBytes;
                try 
                {
                     cleanedFileBytes = await CallNoiseCancellationAsync(mergedFilePath);
                }
                catch (Exception ex)
                {
                    // Fallback or rethrow? Keeping with controller logic (rethrow wrapped or log)
                    // For now, let's allow it to fail if noise cancellation is critical, or fallback to original.
                    // Controller implementation seemed to just proceed or fail. 
                    // Let's assume critical.
                    throw new Exception($"Noise cancellation failed: {ex.Message}", ex);
                }

                // Step 4b: Segmentation
                int wordCount = await CallSegmentationAsync(cleanedFileBytes);

                return (wordCount, mergedFilePath);

            }
            finally
            {
                // 5. Cleanup Input Files (New recordings only)
                foreach (var file in savedFiles)
                {
                    if (System.IO.File.Exists(file))
                    {
                        System.IO.File.Delete(file);
                    }
                }
            }
        }

        public async Task<string> CalculateSSIAsync(string userId, SSIDetectionRequestDto request, string contentRootPath)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new Exception("User not found");
            
            var userDto = await _authServices.GetCurrentUserAsync(user.Email);
            int age = userDto.ChildAge ?? 0;

             // 1. Setup Paths
            var uploadFolder = Path.Combine(contentRootPath, UploadFolderName);
            // Default to 'merged.wav' for SSI as it combines everything? 
            // Or should it be specific? The user said "ssi part 1... takes folders... returned from segmentation"
            // And "merged.wav" is the culmination of the session.
            // Let's use "merged.wav" unless specified otherwise.
            // Actually, if we have "merged_reading.wav" and "merged_images.wav", which one?
            // "Part 1... takes the 2 folders output from segmentation"
            // Wait, my segmentation currently returns a COUNT.
            // CallSegmentationAndGetZipAsync returns the ZIP containing the folders.
            
            // We should use the MAIN merged file.
            // If the user did both, we might want to combine them?
            // For now, let's assume "merged.wav" is the master file or the latest one.
            // If the user Flow is: Images -> Reading -> merged.wav?
            // Or separate?
            // The logic in ProcessVoiceSession merges into: "merged_images.wav", "merged_reading.wav", or "merged.wav".
            // If I am calculating SSI for the child, I probably want ALL their speech.
            // Let's assume for now we pick "merged.wav" if it exists, or fallbacks.
            // Actually, code in ProcessVoiceSessionAsync handles "type".
            // If I am at the end, I might have multiple files.
            // BUT, usually "merged.wav" is the generic one.
            // Let's check if "merged.wav" exists.
            
            string mergedFilePath = Path.Combine(uploadFolder, "merged.wav");
            // If main doesn't exist, try others?
            if (!System.IO.File.Exists(mergedFilePath))
            {
                 // Try individual
                 string reading = Path.Combine(uploadFolder, "merged_reading.wav");
                 if (System.IO.File.Exists(reading)) mergedFilePath = reading;
                 else 
                 {
                     string images = Path.Combine(uploadFolder, "merged_images.wav");
                     if (System.IO.File.Exists(images)) mergedFilePath = images;
                     else throw new Exception("No audio recordings found to analyze.");
                 }
            }

            // 2. Noise Cancellation
            byte[] cleanedFileBytes = await CallNoiseCancellationAsync(mergedFilePath);

            // 3. Segmentation (Get Zip)
            byte[] zipBytes = await CallSegmentationAndGetZipAsync(cleanedFileBytes);

            // 4. SSI Part 1
            string ssi1Json = await CallSSIPart1Async(zipBytes, age, request);

            // 5. SSI Part 2
            string finalJson = await CallSSIPart2Async(ssi1Json, request.IsReader);

            // Save to DB
            try 
            {
                using (JsonDocument doc = JsonDocument.Parse(finalJson))
                {
                    var root = doc.RootElement;
                    string severity = "Unknown";
                    string labelCountsParams = "{}";

                    // Handle different JSON structures for Severity
                    if (root.TryGetProperty("severity", out JsonElement severityElement))
                    {
                        if (severityElement.ValueKind == JsonValueKind.String)
                        {
                            severity = severityElement.GetString();
                        }
                        else if (severityElement.ValueKind == JsonValueKind.Object && severityElement.TryGetProperty("level", out JsonElement levelElement))
                        {
                             severity = levelElement.GetString();
                        }
                    }

                    // Handle different JSON structures for Label Counts
                    if (root.TryGetProperty("label_counts", out JsonElement labelCountsElement))
                    {
                        labelCountsParams = labelCountsElement.GetRawText();
                    }
                    else if (root.TryGetProperty("summary", out JsonElement summaryElement) && summaryElement.TryGetProperty("label_counts", out JsonElement summaryLabelCounts))
                    {
                        labelCountsParams = summaryLabelCounts.GetRawText();
                    }

                    var record = new DiagnosticRecord
                    {
                        UserId = userId,
                        Severity = severity ?? "Unknown",
                        LabelCountsJson = labelCountsParams,
                        FullResultJson = finalJson,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _diagnosticRepository.AddDiagnosticRecordAsync(record);
                }
            }
            catch (Exception ex)
            {
               // Log error (Console for now)
               Console.WriteLine($"Error saving diagnostic record: {ex.Message}");
            }

            return finalJson;
        }

        public async Task CleanupMergedFileAsync(string type, string contentRootPath)
        {
             var uploadFolder = Path.Combine(contentRootPath, UploadFolderName);
             string mergedFileName = type switch
            {
                "images" => "merged_images.wav",
                "reading" => "merged_reading.wav",
                _ => "merged.wav"
            };
            var mergedFilePath = Path.Combine(uploadFolder, mergedFileName);

            if (System.IO.File.Exists(mergedFilePath))
            {
                await Task.Run(() => System.IO.File.Delete(mergedFilePath));
            }
        }

        private async Task<byte[]> CallNoiseCancellationAsync(string filePath)
        {
            using var fileContent = new ByteArrayContent(await System.IO.File.ReadAllBytesAsync(filePath));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav"); // Changed from multipart/form-data to audio/wav for the part

            using var formData = new MultipartFormDataContent();
            // Previous Controller used "merged_input.wav"
            formData.Add(fileContent, "file", "merged_input.wav"); 

            var response = await _httpClient.PostAsync(NoiseCancellationUrl, formData);
            
            if (!response.IsSuccessStatusCode)
            {
                 var error = await response.Content.ReadAsStringAsync();
                 throw new Exception($"Noise Cancellation Failed: {response.ReasonPhrase} - {error}");
            }

            return await response.Content.ReadAsByteArrayAsync();
        }



         private async Task<int> CallSegmentationAsync(byte[] audioBytes)
        {
            using var fileContent = new ByteArrayContent(audioBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav"); 
            using var formData = new MultipartFormDataContent();
            formData.Add(fileContent, "file", "cleaned.wav");
            var response = await _httpClient.PostAsync(SegmentationUrl, formData);
            if (!response.IsSuccessStatusCode)
            {
                 var error = await response.Content.ReadAsStringAsync();
                 throw new Exception($"Segmentation Failed: {response.ReasonPhrase} - {error}");
            }
            using var zipStream = await response.Content.ReadAsStreamAsync();
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
            int count = 0;
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(entry.Name))
                {
                    count++;
                }
            }
            return count;
        }

         private async Task<byte[]> CallSegmentationAndGetZipAsync(byte[] audioBytes)
        {
            using var fileContent = new ByteArrayContent(audioBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav"); 
            using var formData = new MultipartFormDataContent();
            formData.Add(fileContent, "file", "cleaned.wav");
            var response = await _httpClient.PostAsync(SegmentationUrl, formData);
            if (!response.IsSuccessStatusCode)
            {
                 var error = await response.Content.ReadAsStringAsync();
                 throw new Exception($"Segmentation Failed: {response.ReasonPhrase} - {error}");
            }
            return await response.Content.ReadAsByteArrayAsync();
        }

        private async Task<string> CallSSIPart1Async(byte[] zipBytes, int age, SSIDetectionRequestDto request)
        {
            // URL depends on Reader/Non-Reader
            string ssiUrl = request.IsReader 
                ? "https://germinant-elease-subminimal.ngrok-free.dev/detect-reader" 
                : "https://germinant-elease-subminimal.ngrok-free.dev/detect-non-reader";

            using var formData = new MultipartFormDataContent();
            
            // Add Zip file
            var speakingContent = new ByteArrayContent(zipBytes);
            speakingContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/zip");
            formData.Add(speakingContent, "speaking_folder", "speaking.zip");
            
            if (request.IsReader)
            {
                var readingContent = new ByteArrayContent(zipBytes);
                readingContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/zip");
                formData.Add(readingContent, "reading_folder", "reading.zip");
            }

            // Add other fields
            formData.Add(new StringContent(age.ToString()), "age");
            formData.Add(new StringContent("0.8"), "threshold"); // Detection threshold fixed at 0.6
            
            // Optional fields (sending 0 or valid value)
            if (request.DistractingSounds > 0) formData.Add(new StringContent(request.DistractingSounds.ToString()), "distracting_sounds");
            if (request.FacialGrimaces > 0) formData.Add(new StringContent(request.FacialGrimaces.ToString()), "facial_grimaces");
            if (request.HeadMovements > 0) formData.Add(new StringContent(request.HeadMovements.ToString()), "head_movements");
            if (request.Extremities > 0) formData.Add(new StringContent(request.Extremities.ToString()), "extremities");

            var response = await _httpClient.PostAsync(ssiUrl, formData);
            if (!response.IsSuccessStatusCode)
            {
                 var error = await response.Content.ReadAsStringAsync();
                 throw new Exception($"SSI Part 1 Failed: {response.ReasonPhrase} - {error}");
            }
            return await response.Content.ReadAsStringAsync();
        }

        private async Task<string> CallSSIPart2Async(string previousJsonResult, bool isReader)
        {
            string ssi2Url = "http://localhost:8001/calculate-ssi";

            using var formData = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(previousJsonResult));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            formData.Add(fileContent, "file", "data.json");

            var response = await _httpClient.PostAsync(ssi2Url, formData);
            
             if (!response.IsSuccessStatusCode)
            {
                 var error = await response.Content.ReadAsStringAsync();
                 throw new Exception($"SSI Part 2 Failed: {response.ReasonPhrase} - {error}");
            }
            return await response.Content.ReadAsStringAsync();
        }
        public async Task<DiagnosticRecord?> GetLatestDiagnosisAsync(string userId)
        {
            return await _diagnosticRepository.GetLatestDiagnosticRecordAsync(userId);
        }
    }
}
