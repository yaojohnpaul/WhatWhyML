using IE.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatWhyML
{
    public class WhatWhyTrainer
    {
        private List<String> noWhatInstances;
        private List<String> yesWhatInstances;
        private List<String> noWhyInstances;
        private List<String> yesWhyInstances;

        private const int beforeCount = 10;
        private const int afterCount = 10;

        private string posTags;

        public WhatWhyTrainer()
        {
            posTags = "{" + String.Join(",", Token.PartOfSpeechTags) + "}";
            reinitializeInstances();
        }

        private void reinitializeInstances()
        {
            noWhatInstances = new List<String>();
            yesWhatInstances = new List<String>();
            noWhyInstances = new List<String>();
            yesWhyInstances = new List<String>();
        }

        public void startTrain()
        {
            string whatPath = @"..\..\what.arff";
            string whyPath = @"..\..\why.arff";

            try
            {
                if (File.Exists(whatPath))
                {
                    File.Delete(whatPath);
                }
                if (File.Exists(whyPath))
                {
                    File.Delete(whyPath);
                }

                using (StreamWriter sw = File.CreateText(whatPath))
                {
                    sw.WriteLine("@relation what");
                    sw.WriteLine("@attribute candidate string");
                    sw.WriteLine("@attribute wordCount NUMERIC");
                    sw.WriteLine("@attribute sentence NUMERIC");
                    sw.WriteLine("@attribute candidateScore NUMERIC");
                    sw.WriteLine("@attribute numWho NUMERIC");
                    sw.WriteLine("@attribute numWhen NUMERIC");
                    sw.WriteLine("@attribute numWhere NUMERIC");

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
                    sw.WriteLine("@attribute what {yes, no}");
                    sw.WriteLine("\n@data");
                }

                using (StreamWriter sw = File.CreateText(whyPath))
                {
                    sw.WriteLine("@relation why");
                    sw.WriteLine("@attribute candidate string");
                    sw.WriteLine("@attribute wordCount NUMERIC");
                    sw.WriteLine("@attribute sentence NUMERIC");
                    sw.WriteLine("@attribute candidateScore NUMERIC");
                    sw.WriteLine("@attribute numWho NUMERIC");
                    sw.WriteLine("@attribute numWhen NUMERIC");
                    sw.WriteLine("@attribute numWhere NUMERIC");

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
                    sw.WriteLine("@attribute why {yes, no}");
                    sw.WriteLine("\n@data");
                }
            }
            catch (Exception e)
            {

            }
        }

        public void endTrain()
        {
            string whatPath = @"..\..\what.arff";
            string whyPath = @"..\..\why.arff";

            try
            {
                if (File.Exists(whatPath))
                {
                    using (StreamWriter sw = File.AppendText(whatPath))
                    {
                        foreach (string yes in yesWhatInstances)
                        {
                            sw.WriteLine(yes);
                        }

                        for (int i = 0; i < Math.Min(noWhatInstances.Count, ((double)yesWhatInstances.Count) * 3); i++)
                        {
                            sw.WriteLine(noWhatInstances[i]);
                        }
                    }
                }

                if (File.Exists(whyPath))
                {
                    using (StreamWriter sw = File.AppendText(whyPath))
                    {
                        foreach (string yes in yesWhyInstances)
                        {
                            sw.WriteLine(yes);
                        }

                        for (int i = 0; i < Math.Min(noWhyInstances.Count, ((double)yesWhyInstances.Count) * 3); i++)
                        {
                            sw.WriteLine(noWhyInstances[i]);
                        }
                    }
                }
            }
            catch (Exception e)
            {

            }
        }

        public void train(String wFeature, List<Token> listTokenizedArticle, List<Candidate> listCandidates)
        {
            string lowerWFeature = wFeature.ToLower();

            try
            {
                String str = null;
                String value = null;
                int wordcount = 0;
                int sentence = 0;
                double candidateScore = 0;
                int numWho = 0;
                int numWhen = 0;
                int numWhere = 0;

                int wordsbefore = 0;
                int endIndex = 0;

                foreach (var candidate in listCandidates)
                {
                    value = candidate.Value;
                    wordcount = candidate.Value.Split(' ').Count();
                    sentence = candidate.Sentence;
                    candidateScore = candidate.Score;
                    numWho = candidate.NumWho;
                    numWhen = candidate.NumWhen;
                    numWhere = candidate.NumWhere;

                    /*!!
                     * INITIAL ATTRIBUTES
                     */
                    str = "\"" + value.Replace("\"", "\\\"") + "\",";
                    str += wordcount + ",";
                    str += sentence + ",";
                    str += candidateScore + ",";
                    str += numWho + ",";
                    str += numWhen + ",";
                    str += numWhere + ",";

                    wordsbefore = candidate.Position - beforeCount;
                    endIndex = candidate.Position + candidate.Length;

                    /*
                     * ADDING WORD STRINGS IN DATASET FOR THE WORDS BEFORE AND AFTER
                    */
                    int ctrBefore = wordsbefore;

                    while (ctrBefore < 1)
                    {
                        str += "?,";
                        ctrBefore++;
                    }
                    while (ctrBefore < candidate.Position)
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
                    while (ctrBefore < candidate.Position)
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

                    if (candidate.IsWhat && lowerWFeature == "what" ||
                        candidate.IsWhy && lowerWFeature == "why")
                    {
                        str += "yes";
                        if (lowerWFeature == "what")
                            yesWhatInstances.Add(str);
                        else if (lowerWFeature == "why")
                            yesWhyInstances.Add(str);
                    }
                    else
                    {
                        str += "no";
                        if (lowerWFeature == "what")
                            noWhatInstances.Add(str);
                        else if (lowerWFeature == "why")
                            noWhyInstances.Add(str);
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
