using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPEAK.Abstraction.IServices
{
    public interface IAudioMerger
    {
        Task<string> MergeAudioFilesAsync(List<string> inputFiles, string outputFilePath);
    }
}
