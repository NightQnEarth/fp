using System.Collections.Generic;
using System.Linq;

namespace TagCloudGenerator.GeneratorCore.CloudVocabularyPreprocessors
{
    public class ExcludingPreprocessor : CloudVocabularyPreprocessor
    {
        private readonly TagCloudContext cloudContext;

        public ExcludingPreprocessor(CloudVocabularyPreprocessor nextPreprocessor,
                                     TagCloudContext cloudContext) : base(nextPreprocessor) =>
            this.cloudContext = cloudContext;

        protected override IEnumerable<string> ProcessVocabulary(IEnumerable<string> words) =>
            words.Where(word => !cloudContext.ExcludedWords.Contains(word));
    }
}