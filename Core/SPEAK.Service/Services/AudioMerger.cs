using Microsoft.VisualBasic;
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
                var readers = new List<WaveFileReader>();
                try
                {
                    // Open all WaveFileReaders to read the input WAV files properly
                    foreach (var file in inputFiles)
                    {
                        readers.Add(new WaveFileReader(file));
                    }

                    // Use the WaveFormat of the first input file to write the merged WAV file
                    var targetFormat = readers[0].WaveFormat;

                    using (var writer = new WaveFileWriter(outputFilePath, targetFormat))
                    {
                        var buffer = new byte[4096];
                        foreach (var reader in readers)
                        {
                            int bytesRead;
                            while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                writer.Write(buffer, 0, bytesRead);
                            }
                        }
                    }
                }
                finally
                {
                    // Clean up and release file locks
                    foreach (var reader in readers)
                    {
                        reader?.Dispose();
                    }
                }
            });

            return outputFilePath;
        }
    }
}
