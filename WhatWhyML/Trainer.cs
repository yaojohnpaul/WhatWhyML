using IE.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IE
{
    class Trainer
    {
        private List<Token> listTokenizedArticle;
        private List<Candidate> listCandidates;
        private List<String> noInstances;
        private List<String> yesInstances;

        /*!!
         * NUMBER OF WORDS TO BE CONSIDERED BEFORE AND AFTER THE CANDIDATE
         */
        private const int beforeCount = 10;
        private const int afterCount = 10;

        /*!!
         * NUMBER OF NO INSTANCES IN PROPORTION TO YES INSTANCES
         */
        private double noToYesCount = 3;

        private string posTags;

        public Trainer()
        {
            listTokenizedArticle = new List<Token>();
            listCandidates = new List<Candidate>();
            posTags = "{" + String.Join(",", Token.PartOfSpeechTags) + "}";
            reinitializeInstances();   
        }

        public void setNoToYesCount(double noToYesCount)
        {
            this.noToYesCount = noToYesCount;
        }

        private void reinitializeInstances()
        {
            noInstances = new List<String>();
            yesInstances = new List<String>();
        }

        public void setTokenizedArticle(List<Token> pTokenizedArticle)
        {
            listTokenizedArticle = pTokenizedArticle;
        }

        public void setCandidates(List<Candidate> pCandidates)
        {
            listCandidates = pCandidates;
        }

        public void trainMany(String wFeature, List<List<Token>> pTokenizedArticleList, List<List<Candidate>> pAllCandidateLists, String fileNameAddition = "")
        {
            reinitializeInstances();

            string path = @"..\..\" + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(wFeature.ToLower()) + fileNameAddition + ".arff";
            string lowerWFeature = wFeature.ToLower();

            if (pTokenizedArticleList.Count != pAllCandidateLists.Count ||
                lowerWFeature != "who" && lowerWFeature != "when" && lowerWFeature != "where")
            {
                return;
            }

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                /*!!
                 * RELATION NAME AND ATTRIBUTE DECLARATION
                 */
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine("@relation " + lowerWFeature);
                    sw.WriteLine("@attribute word string");
                    sw.WriteLine("@attribute wordCount NUMERIC");
                    sw.WriteLine("@attribute sentence NUMERIC");
                    sw.WriteLine("@attribute position NUMERIC");
                    sw.WriteLine("@attribute sentenceStartProximity NUMERIC");
                    sw.WriteLine("@attribute wordScore NUMERIC");

                    for (int c = beforeCount; c > 0; c--)
                    {
                        sw.WriteLine("@attribute word-" + c + " string");
                    }
                    for (int c = 1; c <= afterCount; c++)
                    {
                        sw.WriteLine("@attribute word+" + c + " string");
                    }
                    for (int c = beforeCount; c > 0; c--)
                    {
                        sw.WriteLine("@attribute postag-" + c + " " + posTags);
                    }
                    for (int c = 1; c <= afterCount; c++)
                    {
                        sw.WriteLine("@attribute postag+" + c + " " + posTags);
                    }
                    sw.WriteLine("@attribute " + lowerWFeature + " {yes, no}");
                    sw.WriteLine("\n@data");
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Error with writing initial line of training dataset.");
            }

            for (int nI = 0; nI < pTokenizedArticleList.Count; nI++)
            {
                setTokenizedArticle(pTokenizedArticleList[nI]);
                setCandidates(pAllCandidateLists[nI]);
                train(wFeature);
            }

            try
            {
                using (StreamWriter sw = File.AppendText(path))
                {
                    foreach (string yes in yesInstances)
                    {
                        sw.WriteLine(yes);
                    }

                    for (int i = 0; i < Math.Min(noInstances.Count, ((double)yesInstances.Count) * noToYesCount); i++)
                    {
                        sw.WriteLine(noInstances[i]);
                    }
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Error with writing initial line of training dataset.");
            }
        }

        

        /// <summary>
        /// Train a new model by creating an .arff file. 
        /// Set isNewFile to false if you're just adding articles to an existing training file.
        /// Otherwise, set isNewFile to true if you're creating a new training file.
        /// </summary>
        private void train(String wFeature)
        {
            string lowerWFeature = wFeature.ToLower();

            try
            {
                String str = null;
                String value = null;
                int wordcount = 0;
                int sentence = 0;
                int position = 0;
                int frequency = 0;
                int endIndex = 0;
                int wordsbefore = 0;
                double sentenceStartProximity = -1.0;

                string[] arrCandidate = null;

                List<List<Token>> segregatedTokenLists = listTokenizedArticle
                    .GroupBy(token => token.Sentence)
                    .Select(tokenGroup => tokenGroup.ToList())
                    .ToList();

                foreach (var candidate in listCandidates)
                {
                    value = candidate.Value;
                    sentence = candidate.Sentence;
                    position = candidate.Position;
                    frequency = candidate.Frequency;
                    arrCandidate = candidate.Value.Split(' ');
                    endIndex = position + arrCandidate.Length - 1;
                    wordcount = arrCandidate.Count();

                    foreach (List<Token> tokenList in segregatedTokenLists)
                    {
                        if (tokenList.Count > 0 && tokenList[0].Sentence == sentence)
                        {
                            sentenceStartProximity = (double)(position - tokenList[0].Position) / (double)tokenList.Count;
                            break;
                        }
                    }

                    wordsbefore = position - beforeCount;

                    /*!!
                     * INITIAL ATTRIBUTES
                     */
                    str = "\"" + value.Replace("\"", "\\\"") + "\",";
                    //str = "";
                    str += wordcount + ",";
                    str += sentence + ",";
                    str += position + ",";
                    str += ((sentenceStartProximity == -1) ? "?" : "" + sentenceStartProximity) + ",";
                    str += frequency + ",";

                    /*
                     * ADDING WORD STRINGS IN DATASET FOR THE WORDS BEFORE AND AFTER
                    */
                    int ctrBefore = wordsbefore;

                    while (ctrBefore < 1)
                    {
                        str += "?,";
                        ctrBefore++;
                    }
                    while (ctrBefore < position)
                    {
                        str += "\"" + listTokenizedArticle[ctrBefore - 1].Value.Replace("\"", "\\\"") + "\",";
                        ctrBefore++;
                    }
                    for (int c = 0; c < afterCount; c++)
                    {
                        if (endIndex + c < listTokenizedArticle.Count)
                        {
                            str += "\"" + listTokenizedArticle[endIndex + c].Value.Replace("\"", "\\\"") + "\",";
                        }
                        else
                        {
                            str += "?,";
                        }
                    }

                    /*
                     * ADDING POS TAGS IN DATASET FOR THE WORDS BEFORE AND AFTER
                     */
                    ctrBefore = wordsbefore;

                    while (ctrBefore < 1)
                    {
                        str += "?,";
                        ctrBefore++;
                    }
                    while (ctrBefore < position)
                    {
                        str += listTokenizedArticle[ctrBefore - 1].PartOfSpeech + ",";
                        ctrBefore++;
                    }
                    for (int c = 0; c < afterCount; c++)
                    {
                        if (endIndex + c < listTokenizedArticle.Count)
                        {
                            str += listTokenizedArticle[endIndex + c].PartOfSpeech + ",";
                        }
                        else
                        {
                            str += "?,";
                        }
                    }

                    if (candidate.IsWho && lowerWFeature == "who" ||
                        candidate.IsWhen && lowerWFeature == "when" ||
                        candidate.IsWhere && lowerWFeature == "where")
                    {
                        str += "yes";
                        yesInstances.Add(str);
                    }
                    else
                    {
                        str += "no";
                        noInstances.Add(str);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
