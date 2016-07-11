using edu.stanford.nlp.ie.crf;
using edu.stanford.nlp.ling;
using edu.stanford.nlp.process;
using edu.stanford.nlp.tagger.maxent;
using IE.Models;
using java.io;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace IE
{
    public class Preprocessor
    {
        private Article articleCurrent;
        private Annotation annotationCurrent;
        private List<Token> listLatestTokenizedArticle;
        private List<Candidate> listWhoCandidates;
        private List<Candidate> listWhenCandidates;
        private List<Candidate> listWhereCandidates;
        private List<List<Token>> listWhatCandidates;
        private List<List<Token>> listWhyCandidates;
        private CRFClassifier nerClassifier;
        private MaxentTagger posTagger;

        private readonly String nerModelPath = @"..\..\NERModel\filipino.ser.gz";
        private readonly String posModelPath = @"..\..\POSTagger\filipino.tagger";

        public Preprocessor()
        {
            listLatestTokenizedArticle = new List<Token>();
            listWhoCandidates = new List<Candidate>();
            listWhenCandidates = new List<Candidate>();
            listWhereCandidates = new List<Candidate>();
            listWhatCandidates = new List<List<Token>>();
            listWhyCandidates = new List<List<Token>>();
            nerClassifier = CRFClassifier.getClassifierNoExceptions(nerModelPath);
            posTagger = new MaxentTagger(posModelPath);
        }

        #region Setters
        public void setCurrentArticle(Article pArticle)
        {
            articleCurrent = pArticle;
        }

        public void setCurrentAnnotation(Annotation pAnnotation)
        {
            annotationCurrent = pAnnotation;
        }
        #endregion

        #region Getters
        public Article getCurrentArticle()
        {
            return articleCurrent;
        }

        public Annotation getCurrentAnnotation()
        {
            return annotationCurrent;
        }

        public List<Token> getLatestTokenizedArticle()
        {
            return listLatestTokenizedArticle;
        }

        public List<Candidate> getWhoCandidates()
        {
            return listWhoCandidates;
        }

        public List<Candidate> getWhenCandidates()
        {
            return listWhenCandidates;
        }

        public List<Candidate> getWhereCandidates()
        {
            return listWhereCandidates;
        }

        public List<List<Token>> getWhatCandidates()
        {
            return listWhatCandidates;
        }

        public List<List<Token>> getWhyCandidates()
        {
            return listWhyCandidates;
        }
        #endregion

        public List<Token> preprocess()
        {
            if (articleCurrent == null)
            {
                return null;
            }

            listLatestTokenizedArticle = new List<Token>();

            performTokenizationAndSS();
            performNER();
            performPOST();
            performWS();
            performCandidateSelection();

            foreach (var token in listLatestTokenizedArticle)
            {
                //System.Console.WriteLine("Value: " + token.Value);
                //System.Console.WriteLine("Sentence: " + token.Sentence);
                //System.Console.WriteLine("Position: " + token.Position);
                //System.Console.WriteLine("NER: " + token.NamedEntity);
                //System.Console.WriteLine("POS: " + token.PartOfSpeech);
                //System.Console.WriteLine("WS: " + token.Frequency);
                //System.Console.WriteLine("=====\n");
            }

            return listLatestTokenizedArticle;
        }

        /// <summary>
        /// Assign an article's token to whether or not it is part of a 5W.
        /// </summary>
        public float[][] performAnnotationAssignment()
        {
            float[][] statistics = new float[5][];
            if (annotationCurrent == null)
            {
                return null;
            }

            statistics[0] = performMultipleAnnotationAssignment("WHO");
            statistics[1] = performMultipleAnnotationAssignment("WHEN");
            statistics[2] = performMultipleAnnotationAssignment("WHERE");
            statistics[3] = performSingleAnnotationAssignment("WHAT");
            statistics[4] = performSingleAnnotationAssignment("WHY");

            return statistics;
        }


        private void performCandidateSelection()
        {
            CandidateSelector selector = new CandidateSelector();
            listWhoCandidates = selector.performWhoCandidateSelection(listLatestTokenizedArticle, articleCurrent.Title);
            listWhenCandidates = selector.performWhenCandidateSelection(listLatestTokenizedArticle, articleCurrent.Title);
            listWhereCandidates = selector.performWhereCandidateSelection(listLatestTokenizedArticle, articleCurrent.Title);
            listWhatCandidates = selector.performWhatCandidateSelection(listLatestTokenizedArticle, articleCurrent.Title);
            listWhyCandidates = selector.performWhyCandidateSelection(listLatestTokenizedArticle, articleCurrent.Title);
        }

        #region Article Preprocessing Functions
        private void performTokenizationAndSS()
        {
            listLatestTokenizedArticle = performTokenizationAndSS(articleCurrent.Body);
        }

        public List<Token> performTokenizationAndSS(String toBeTokenized)
        {
            List<Token> tokenizedString = new List<Token>();
            var sentences = MaxentTagger.tokenizeText(new java.io.StringReader(toBeTokenized)).toArray();
            int sentenceCounter = 1;
            int positionCounter = 1;
            String[] abbreviationList = new String[] {
            "Dr", //Names
            "Dra",
            "Gng",
            "G",
            "Gg",
            "Bb",
            "Esq",
            "Jr",
            "Mr",
            "Mrs",
            "Ms",
            "Messrs",
            "Mmes",
            "Msgr",
            "Prof",
            "Rev",
            "Pres",
            "Sec",
            "Sr",
            "Fr",
            "St",
            "Hon",
            "Ave", //Streets
            "Aly",
            "Gen", //Military Rank
            "1Lt",
            "2Lt",
            "Cpt",
            "Maj",
            "Capt",
            "1stLt",
            "2ndLt",
            "Adm",
            "W01",
            "CW2",
            "CW3",
            "CW4",
            "CW5",
            "Col",
            "LtCol",
            "BG",
            "MG",
            "Sgt",
            "SSgt",
            "LCpl",
            "SgtMaj",
            "1stSgt",
            "1Sgt",
            "Pvt"
            };
            foreach (java.util.ArrayList sentence in sentences)
            {
                String wordFinal = "";
                foreach (var word in sentence)
                {
                    var newToken = new Token(word.ToString(), positionCounter);
                    newToken.Sentence = sentenceCounter;
                    tokenizedString.Add(newToken);
                    positionCounter++;
                    if(!newToken.Value.Equals("."))
                        wordFinal = word.ToString();
                }
                Boolean flag = true;
                foreach(String word in abbreviationList)
                {
                    if (wordFinal.Equals(word))
                        flag = false;
                }
                if (flag)
                    sentenceCounter++;
            }
            return tokenizedString;
        }

        private void performNER()
        {
            java.util.List tokens;
            List<string> values = new List<string>();
            object[] nerValues;

            foreach (Token token in listLatestTokenizedArticle)
            {
                values.Add(token.Value);
            }

            tokens = Sentence.toCoreLabelList(values.ToArray());

            nerValues = nerClassifier.classifySentence(tokens).toArray();

            for (int i = 0; i < listLatestTokenizedArticle.Count; i++)
            {
                listLatestTokenizedArticle[i].NamedEntity = ((CoreLabel)nerValues[i]).get(typeof(CoreAnnotations.AnswerAnnotation)).ToString();
            }
        }

        private void performPOST()
        {
            //Get all tokens and segregate them into lists based on sentence number
            List<List<Token>> segregatedTokenLists = listLatestTokenizedArticle
                .GroupBy(token => token.Sentence)
                .Select(tokenGroup => tokenGroup.ToList())
                .ToList();

            //Convert the lists into a "CoreLabelList" and store in a Dictionary
            //Dictionary Key: Sentence Number
            //Dictionary Value: CoreLabelList
            Dictionary<int, java.util.List> tokenizedSentenceLists = new Dictionary<int, java.util.List>();
            foreach (List<Token> tokenList in segregatedTokenLists)
            {
                if (tokenList.Count > 0)
                {
                    var tokenToStringArray = tokenList.Select(token => token.Value).ToArray();
                    tokenizedSentenceLists[tokenList[0].Sentence] = Sentence.toCoreLabelList(tokenToStringArray);
                }
            }

            //Tag each sentence
            foreach (KeyValuePair<int, java.util.List> entry in tokenizedSentenceLists)
            {
                var taggedSentence = posTagger.tagSentence(entry.Value).toArray();
                foreach (var word in taggedSentence)
                {
                    var splitWord = word.ToString().Split('/');
                    if (splitWord.Length >= 2)
                    {
                        foreach (var token in listLatestTokenizedArticle)
                        {
                            if ((token.PartOfSpeech == null || token.PartOfSpeech.Length <= 0) &&
                                token.Value.Trim() == splitWord[0].Trim() &&
                                token.Sentence == entry.Key)
                            {
                                token.PartOfSpeech = splitWord[1];
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void performWS()
        {
            Dictionary<string, int> frequencies = new Dictionary<string, int>();

            foreach (Token token in listLatestTokenizedArticle)
            {
                if (frequencies.ContainsKey(token.Value))
                {
                    frequencies[token.Value]++;
                }
                else
                {
                    frequencies[token.Value] = 1;
                }
            }

            foreach (Token token in listLatestTokenizedArticle)
            {
                token.Frequency = frequencies[token.Value];
            }
        }
        #endregion

        #region Annotation Preprocessing Functions
        private float[] performSingleAnnotationAssignment(String annotationType)
        {
            float[] statistics = new float[4] { 0, 0, 0, -1 }; //[0] = recall, [1] = precision, [2]  total, [3] sentence number
            Boolean totalMatch = false;
            annotationType = annotationType.ToUpper();
            String strAnnotation = "";
            if (annotationType == "WHAT")
            {
                strAnnotation = String.Copy(annotationCurrent.What);
                //System.Console.WriteLine("WHAT Annotation: " + strAnnotation);
            }
            else if (annotationType == "WHY")
            {
                strAnnotation = String.Copy(annotationCurrent.Why);
                //System.Console.WriteLine("WHY Annotation: " + strAnnotation);
            }
            if (annotationType != "WHAT" && annotationType != "WHY" || strAnnotation.Count() <= 0 || strAnnotation == "N/A")
            {
                return statistics;
            }
            String original = strAnnotation;
            strAnnotation = strAnnotation.Replace("-LRB- ", "(");
            strAnnotation = strAnnotation.Replace(" -RRB-", ")");
            strAnnotation = strAnnotation.Replace("''", "");
            strAnnotation = strAnnotation.Replace("\"", "");
            strAnnotation = strAnnotation.Replace(" ,", ",");
            strAnnotation = strAnnotation.Replace(" !", "!");
            Regex rgx = new Regex("[^a-zA-Z0-9]");
            strAnnotation = rgx.Replace(strAnnotation, "");


            if (annotationType == "WHAT")
            {
                if (strAnnotation != "")
                {
                    statistics[2] = 1;
                    foreach (var candidate in listWhatCandidates)
                    {
                        //System.Console.WriteLine("WHAT CANDIDATES: " + string.Join(" ", candidate.Select(x => x.Value).ToArray()));
                        var tempCandidate = string.Join("", candidate.Select(x => x.Value).ToArray());
                        tempCandidate = tempCandidate.Replace("-LRB-", "(");
                        tempCandidate = tempCandidate.Replace("-RRB-", ")");
                        tempCandidate = tempCandidate.Replace("''", "");
                        tempCandidate = tempCandidate.Replace("\"", "");
                        tempCandidate = tempCandidate.Replace("``", "");
                        tempCandidate = tempCandidate.Replace(" !", "!");
                        tempCandidate = rgx.Replace(tempCandidate, "");
                        if (tempCandidate.Equals(strAnnotation, StringComparison.OrdinalIgnoreCase))
                        {
                            totalMatch = true;
                            statistics[3] = candidate.ElementAt(0).Sentence;
                            break;
                        }
                        else if (strAnnotation.Contains(tempCandidate))
                        {
                            totalMatch = true;
                            statistics[3] = candidate.ElementAt(0).Sentence;
                            //System.Console.WriteLine("'WHAT' Under-extracted: " + tempCandidate + " - " + strAnnotation);
                        }
                        else if (tempCandidate.Contains(strAnnotation))
                        {
                            totalMatch = true;
                            statistics[3] = candidate.ElementAt(0).Sentence;
                            //System.Console.WriteLine("'WHAT' Over-extracted: " + candidate.Value + " - " + annotation);
                        }
                        else
                        {
                            //System.Console.WriteLine("'WHAT' Complete Mismatch: " + candidate.Value + " - " + annotation);
                        }
                    }
                    if (statistics[3] < 0)
                    {
                        //System.Console.WriteLine("NO MATCH: " + original);
                    }
                    statistics[0] = totalMatch ? 1 : 0;
                    statistics[1] = statistics[0] / listWhatCandidates.Count;
                }
            }
            else if (annotationType == "WHY")
            {
                if (strAnnotation != "")
                {
                    statistics[2] = 1;
                    foreach (var candidate in listWhyCandidates)
                    {
                        //System.Console.WriteLine("WHY CANDIDATES: " + string.Join(" ", candidate.Select(x => x.Value).ToArray()));
                        var tempCandidate = string.Join("", candidate.Select(x => x.Value).ToArray());
                        tempCandidate = tempCandidate.Replace("-LRB-", "(");
                        tempCandidate = tempCandidate.Replace("-RRB-", ")");
                        tempCandidate = tempCandidate.Replace(" . ", ".");
                        tempCandidate = tempCandidate.Replace(" .", ".");
                        tempCandidate = tempCandidate.Replace(" ,", ",");
                        tempCandidate = tempCandidate.Replace(" !", "!");
                        tempCandidate = rgx.Replace(tempCandidate, "");
                        if (tempCandidate.Equals(strAnnotation, StringComparison.OrdinalIgnoreCase))
                        {
                            totalMatch = true;
                            statistics[3] = candidate.ElementAt(0).Sentence;
                            break;
                        }
                        else if (strAnnotation.Contains(tempCandidate))
                        {
                            totalMatch = true;
                            statistics[3] = candidate.ElementAt(0).Sentence;
                            //System.Console.WriteLine("'WHY' Under-extracted: " + tempCandidate + " - " + strAnnotation);
                        }
                        else if (tempCandidate.Contains(strAnnotation))
                        {
                            totalMatch = true;
                            statistics[3] = candidate.ElementAt(0).Sentence;
                            //System.Console.WriteLine("'WHY' Over-extracted: " + candidate.Value + " - " + annotation);
                        }
                        else
                        {
                            //System.Console.WriteLine("'WHY' Complete Mismatch: " + candidate.Value + " - " + annotation);
                        }
                    }
                    if(statistics[3] < 0)
                    {
                        //System.Console.WriteLine("NO MATCH: " + original);
                    }
                    statistics[0] = totalMatch ? 1 : 0;
                    statistics[1] = statistics[0] / listWhyCandidates.Count;
                }
            }

            return statistics;
        }

        private float[] performMultipleAnnotationAssignment(String annotationType)
        {
            float[] statistics = new float[3] { 0, 0, 0 }; //[0] = recall, [1] = precision, [2]  total
            int totalMatch = 0;
            annotationType = annotationType.ToUpper();
            if (annotationType != "WHO" && annotationType != "WHEN" && annotationType != "WHERE")
            {
                return statistics;
            }

            String strAnnotation = "";
            Action<string> assignmentMethod = null;
            string[] arrAnnotations = null;
            bool foundMatchingCandidate = false;
            switch (annotationType)
            {
                case "WHO":
                    strAnnotation = annotationCurrent.Who;
                    if (strAnnotation != null)
                    {
                        //System.Console.WriteLine("WHO Annotation: " + strAnnotation);
                        assignmentMethod = annotation =>
                        {
                            foreach (var candidate in listWhoCandidates)
                            {
                                if (candidate.Value == annotation)
                                {
                                    candidate.IsWho = true;
                                    foundMatchingCandidate = true;
                                    //System.Console.WriteLine("WHO\nBEFORE: " + (((candidate.Position - 2) >= 0) ? listLatestTokenizedArticle[candidate.Position - 2].Value : "N/A"));
                                    //string[] temp = candidate.Value.Split(' ');
                                    //System.Console.WriteLine("AFTER: " + (((candidate.Position + temp.Length - 1) <= listLatestTokenizedArticle.Count()) ? listLatestTokenizedArticle[candidate.Position + temp.Length - 1].Value : "N/A"));
                                    break;
                                }
                                else if (annotation.Contains(candidate.Value))
                                {

                                    //System.Console.WriteLine("'WHO' Under-extracted: " + candidate.Value + " - " + annotation);
                                }
                                else if (candidate.Value.Contains(annotation))
                                {
                                    //System.Console.WriteLine("'WHO' Over-extracted: " + candidate.Value + " - " + annotation);
                                }
                                else
                                {
                                    //System.Console.WriteLine("'WHO' Complete Mismatch: " + candidate.Value + " - " + annotation);
                                }
                            }
                        };
                    }
                    break;
                case "WHEN":
                    strAnnotation = annotationCurrent.When;
                    if (strAnnotation != null)
                    {
                        //System.Console.WriteLine("WHEN Annotation: " + strAnnotation);
                        assignmentMethod = annotation =>
                        {
                            foreach (var candidate in listWhenCandidates)
                            {
                                if (candidate.Value == annotation)
                                {
                                    candidate.IsWhen = true;
                                    foundMatchingCandidate = true;
                                    //System.Console.WriteLine("WHEN\nBEFORE: " + (((candidate.Position - 2) >= 0) ? listLatestTokenizedArticle[candidate.Position - 2].Value : "N/A"));
                                    //string[] temp = candidate.Value.Split(' ');
                                    //System.Console.WriteLine("AFTER: " + (((candidate.Position + temp.Length - 1) <= listLatestTokenizedArticle.Count()) ? listLatestTokenizedArticle[candidate.Position + temp.Length - 1].Value : "N/A"));
                                    break;
                                }
                                else if (annotation.Contains(candidate.Value))
                                {
                                    //System.Console.WriteLine("'WHEN' Under-extracted: " + candidate.Value + " - " + annotation);
                                }
                                else if (candidate.Value.Contains(annotation))
                                {
                                    //System.Console.WriteLine("'WHEN' Over-extracted: " + candidate.Value + " - " + annotation);
                                }
                                else
                                {
                                    //System.Console.WriteLine("'WHEN' Complete Mismatch: " + candidate.Value + " - " + annotation);
                                }
                            }
                        };
                    }
                    break;
                case "WHERE":
                    strAnnotation = annotationCurrent.Where;
                    if (strAnnotation != null)
                    {
                        //System.Console.WriteLine("WHERE Annotation: " + strAnnotation);
                        assignmentMethod = annotation =>
                        {
                            foreach (var candidate in listWhereCandidates)
                            {
                                if (candidate.Value == annotation)
                                {
                                    candidate.IsWhere = true;
                                    foundMatchingCandidate = true;
                                    //System.Console.WriteLine("WHERE\nBEFORE: " + (((candidate.Position - 2) >= 0) ? listLatestTokenizedArticle[candidate.Position - 2].Value : "N/A"));
                                    //string[] temp = candidate.Value.Split(' ');
                                    //System.Console.WriteLine("AFTER: " + (((candidate.Position + temp.Length - 1) <= listLatestTokenizedArticle.Count()) ? listLatestTokenizedArticle[candidate.Position + temp.Length - 1].Value : "N/A"));
                                    break;
                                }
                                else if (annotation.Contains(candidate.Value))
                                {
                                    //System.Console.WriteLine("'WHERE' Under-extracted: " + candidate.Value + " - " + annotation);
                                }
                                else if (candidate.Value.Contains(annotation))
                                {
                                    //System.Console.WriteLine("'WHERE' Over-extracted: " + candidate.Value + " - " + annotation);
                                }
                                else
                                {
                                    //System.Console.WriteLine("'WHERE' Complete Mismatch: " + candidate.Value + " - " + annotation);
                                }
                            }
                        };
                    }
                    break;
            }

            if (strAnnotation.Count() <= 0 || strAnnotation == "N/A")
            {
                return statistics;
            }

            arrAnnotations = strAnnotation.Split(';');

            for (int r = 0; r < arrAnnotations.Length; r++)
            {
                if (arrAnnotations[r].Length > 0 && arrAnnotations[r][0] == ' ')
                {
                    arrAnnotations[r] = arrAnnotations[r].Substring(1);
                }

                ////System.Console.WriteLine(annotationType + " ANNOTATIONS-" + arrAnnotations[r]);
                if (assignmentMethod != null)
                {
                    assignmentMethod(arrAnnotations[r]);
                }

                if (!foundMatchingCandidate)
                {
                    int i = -1;
                    String[] wordForWordAnnotation = arrAnnotations[r].Split(' ');
                    for (int ctr = 0; ctr < listLatestTokenizedArticle.Count; ctr++)
                    {
                        if (wordForWordAnnotation[0].Contains(listLatestTokenizedArticle[ctr].Value))
                        {
                            i = ctr;
                            break;
                        }
                    }

                    if (i > -1)
                    {
                        //add as candidate
                        int startIndex = i;
                        int tempWs = listLatestTokenizedArticle[i].Frequency;

                        for (int ctr = startIndex; ctr < startIndex + wordForWordAnnotation.Count(); ctr++)
                        {
                            if (ctr < listLatestTokenizedArticle.Count && listLatestTokenizedArticle[ctr].Frequency > tempWs)
                            {
                                tempWs = listLatestTokenizedArticle[ctr].Frequency;
                            }
                        }

                        var newToken = new Candidate(arrAnnotations[r], listLatestTokenizedArticle[startIndex].Position, startIndex + wordForWordAnnotation.Count() - 1);
                        newToken.Sentence = listLatestTokenizedArticle[i].Sentence;
                        newToken.NamedEntity = listLatestTokenizedArticle[i].NamedEntity;
                        newToken.PartOfSpeech = listLatestTokenizedArticle[i].PartOfSpeech;
                        newToken.Frequency = tempWs;
                        switch (annotationType)
                        {
                            case "WHO":
                                newToken.IsWho = true;
                                listWhoCandidates.Add(newToken);
                                break;
                            case "WHEN":
                                newToken.IsWhen = true;
                                listWhenCandidates.Add(newToken);
                                break;
                            case "WHERE":
                                newToken.IsWhere = true;
                                listWhereCandidates.Add(newToken);
                                break;
                        }
                    }
                }
                else
                {
                    totalMatch += 1;
                    foundMatchingCandidate = false;
                }
            }

            //System.Console.WriteLine("Annotations Count: {0}", arrAnnotations.GetLength(0));
            statistics[2] += 1;
            statistics[0] = (float)totalMatch / arrAnnotations.GetLength(0);
            switch (annotationType)
            {
                case "WHO":
                    statistics[1] = (float)totalMatch / listWhoCandidates.Count;
                    //System.Console.WriteLine("Total Match: {0}, Who Candidates Count: {1}", totalMatch, listWhoCandidates.Count);
                    break;
                case "WHEN":
                    statistics[1] = (float)totalMatch / listWhenCandidates.Count;
                    //System.Console.WriteLine("Total Match: {0}, When Candidates Count: {1}", totalMatch, listWhenCandidates.Count);
                    break;
                case "WHERE":
                    statistics[1] = (float)totalMatch / listWhereCandidates.Count;
                    //System.Console.WriteLine("Total Match: {0}, Where Candidates Count: {1}", totalMatch, listWhereCandidates.Count);
                    break;
            }
            return statistics;
        }
        #endregion
    }
}
