using SPEAK.Abstraction.IServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPEAK.Service.Services
{
    public class AudioMerger : IAudioMerger
    {
        public async Task<string> MergeAudioFilesAsync(List<string> inputFiles, string outputFilePath)
        {
            if (inputFiles == null || inputFiles.Count == 0)
                throw new ArgumentException("No input files provided.");

            // 👇 حطي مسار ffmpeg.exe عندك هنا
            var ffmpegPath = @"F:\ffmpeg-8.0.1-essentials_build\bin\ffmpeg.exe";

            var tempFiles = new List<string>();

            // 🔹 1) تحويل كل ملف مؤقتًا لـ WAV
            foreach (var filePath in inputFiles)
            {
                var tempWav = Path.Combine(
                    Path.GetTempPath(),
                    Path.GetFileNameWithoutExtension(filePath) + "_" + Guid.NewGuid() + "_temp.wav"
                );

                var convertProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = $"-y -i \"{filePath}\" -ac 1 -ar 16000 -c:a pcm_s16le \"{tempWav}\"",
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                convertProcess.Start();

                string convertError = await convertProcess.StandardError.ReadToEndAsync();
                await convertProcess.WaitForExitAsync();

                if (convertProcess.ExitCode != 0)
                    throw new Exception("FFmpeg Convert Error: " + convertError);

                tempFiles.Add(tempWav);
            }

            // 🔹 2) إنشاء list.txt للـ concat
            var listFilePath = Path.Combine(
                Path.GetTempPath(),
                "list_" + Guid.NewGuid() + ".txt"
            );

            await File.WriteAllLinesAsync(
                listFilePath,
                tempFiles.Select(f => $"file '{f.Replace("\\", "/")}'")
            );

            // 🔹 3) دمج الملفات
            var mergeProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-y -f concat -safe 0 -i \"{listFilePath}\" -ac 1 -ar 16000 -c:a pcm_s16le \"{outputFilePath}\"",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            mergeProcess.Start();

            string mergeError = await mergeProcess.StandardError.ReadToEndAsync();
            await mergeProcess.WaitForExitAsync();

            if (mergeProcess.ExitCode != 0)
                throw new Exception("FFmpeg Merge Error: " + mergeError);

            foreach (var temp in tempFiles)
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }

            if (File.Exists(listFilePath))
                File.Delete(listFilePath);

            return outputFilePath;
        }
    }
}
