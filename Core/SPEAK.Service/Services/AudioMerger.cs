using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SPEAK.Abstraction.IServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SPEAK.Service.Services
{
    public class AudioMerger : IAudioMerger
    {
        public async Task<string> MergeAudioFilesAsync(List<string> inputFiles, string outputFilePath)
        {
            if (inputFiles == null || inputFiles.Count == 0)
                throw new ArgumentException("No input files provided.");

            // Run on background thread to keep API responsive
            await Task.Run(() =>
            {
                var streams = new List<FileStream>();
                var providers = new List<ISampleProvider>();

                try
                {
                    foreach (var file in inputFiles)
                    {
                        var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                        streams.Add(fs);
                        
                        // Treat the input purely as raw 16kHz Mono 16-bit PCM.
                        // This bypasses any RIFF header validation errors (Not a WAVE file).
                        // If a header exists, it's just treated as 0.002s of noise, which Modal will clean.
                        var rawReader = new RawSourceWaveStream(fs, new WaveFormat(16000, 16, 1));
                        providers.Add(rawReader.ToSampleProvider());
                    }

                    // Safely concatenate the audio data using NAudio's managed logic
                    var playlist = new ConcatenatingSampleProvider(providers);

                    // Write to output file
                    WaveFileWriter.CreateWaveFile16(outputFilePath, playlist);
                }
                finally
                {
                    // Clean up resources to release file locks
                    foreach (var stream in streams)
                    {
                        stream?.Dispose();
                    }
                }
            });

            return outputFilePath;
        }
    }
}
