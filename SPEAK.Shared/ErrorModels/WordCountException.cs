using System;

namespace SPEAK.Shared.ErrorModels
{
    public class WordCountException : Exception
    {
        public string MessageEn { get; }
        public string MessageAr { get; }
        public int WordCount { get; }

        public WordCountException(string messageEn, string messageAr, int wordCount)
            : base(messageEn)
        {
            MessageEn = messageEn;
            MessageAr = messageAr;
            WordCount = wordCount;
        }
    }
}
