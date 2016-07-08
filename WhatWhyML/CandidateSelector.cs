using IE.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IE
{
    class CandidateSelector
    {
        public List<Candidate> performWhoCandidateSelection(List<Token> tokenizedArticle, String articleTitle)
        {
            List<Candidate> candidates = new List<Candidate>();
            List<Candidate> temporaryCandidates = new List<Candidate>();
            String[] startMarkers = new String[3] { "si",
                //"sina",
                //"kay",
                //"ni",
                "ang",
                "ng",
                /*"sa"*/};
            String[][] endMarkers = new String[3][] { new String[] { "na", "ng", ".", "bilang", "ang", "kamakalawa", "alyas", "at", "kay", ",", "sa", "makaraang", "mula"},
                /*new String[] { "at"},
                new String[] { "ng", "na"},
                new String[] { "ng", ",", "na", "ang"},*/
                new String[] { "na", "sa", "kay", "at", "ng", "makaraang", "para", "nang", "ang", "-LRB-", "mula"},
                new String[] { "ng", "ang", "si", "ang", ".", "para", "at", "na", "sa", "-LRB-", "mula"},
                /*new String[] { "sa", "na", "kaugnay", "ang", "upang", ",", ".", "-LRB-"}*/ }; 
            String[][] enderMarkers = new String[3][] { new String[] { "dahil", "kapag", "noong"},
                new String[] { "dahil", "kapag", "noong"},
                /*new String[] { "dahil", "kapag", "noong"},
                new String[] { "dahil", "kapag", "noong"},
                new String[] { "dahil", "kapag", "noong"},
                new String[] { "dahil", "kapag", "noong"},*/
                new String[] { "dahil", "kapag", "noong"}}; // Add all why start markers

            for (int i = 0; i < tokenizedArticle.Count; i++)
            {
                i = getCandidateByNer("PER", i, candidates, tokenizedArticle);
                i = getCandidateByNer("ORG", i, candidates, tokenizedArticle);
                getCandidateByMarkers(null, startMarkers, endMarkers, enderMarkers, null, null, i, temporaryCandidates, tokenizedArticle, true);

                if (tokenizedArticle[i].Sentence > 3)
                {
                    break;
                }
            }

            foreach (Candidate candidate in temporaryCandidates)
            {
                if (candidate.Value == null) continue;
                double candidateWeight = 0;
                int numWords = candidate.Value.Split(' ').Count();
                candidateWeight += 1 - (numWords / 5 + 1) * 0.2;
                if (candidate.Value.StartsWith("mga"))
                {
                    candidateWeight += 0.7;
                }
                if (articleTitle.Contains(candidate.Value))
                {
                    candidateWeight += 0.7;
                }

                bool found = false;
      
                for (int currentIndex = candidate.Position - 1; currentIndex < candidate.Position + candidate.Length - 1; currentIndex++)
                {
                    //Console.WriteLine(tokenizedArticle[currentIndex].PartOfSpeech);
                    if (tokenizedArticle[currentIndex].PartOfSpeech != null && (tokenizedArticle[currentIndex].PartOfSpeech.StartsWith("V") || tokenizedArticle[currentIndex].PartOfSpeech.StartsWith("PR") || tokenizedArticle[currentIndex].PartOfSpeech.StartsWith("RB")))
                    {
                        Console.WriteLine(tokenizedArticle[currentIndex].PartOfSpeech);
                        candidateWeight = 0;
                        break;
                    }
                    /*if (tokenizedArticle[currentIndex].PartOfSpeech.StartsWith("N") && !found)
                    {
                        //Console.WriteLine("was here"+ candidateWeight);
                        candidateWeight += 0.3;
                        found = true;
                    }*/
                }


                if (candidateWeight >= 1)
                {
                    candidates.Add(candidate);
                }
            }
            //for (int i = 0; i < tokenizedArticle.Count; i++)
            //{
            //    i = getCandidateByPos("NNC", i, candidates, tokenizedArticle);
            //}

            for (int can = 0; can < candidates.Count; can++)
            {
                for (int a = 0; a < can; a++)
                {
                    if (candidates[can].Value != null && candidates[a].Value != null && candidates[can].Value.Equals(candidates[a].Value))
                    {
                        candidates.RemoveAt(can);
                        if (can > 0)
                        {
                            can--;
                        }
                        break;
                    }
                }
            }

            foreach (var candidate in candidates)
            {
                candidate.Value = candidate.Value.Replace("`` ", "\"");
                candidate.Value = candidate.Value.Replace("``", "\"");
                candidate.Value = candidate.Value.Replace(" ''", "\"");
                candidate.Value = candidate.Value.Replace("''", "\"");
                System.Console.WriteLine("WHO CANDIDATE " + candidate.Value);
            }

            return candidates;
        }

        public List<Candidate> performWhenCandidateSelection(List<Token> tokenizedArticle, String articleTitle)
        {
            List<Candidate> candidates = new List<Candidate>();
            String[] generalStopWords = new string[] { "mga", 
                "dahil",
                "dahilan",
                "subalit",
                "makaraang",
                "kaya",
                "kung",
                "kapag",
                "ngunit",
                "palibhassa",
                "sapagkat",
                "sana",
                "kundi",
                "ni",
                "naglalayong" };
            String[] startMarkersExclusive = new String[] { "ang",
                "mula",
                "na",
                "noong",
                "nuong",
                "sa" };
            String[][] endMarkersExclusive = new String[][] { new String[] { "para", "upang"},
                new String[] { "upang", "para", ",", "."},
                new String[] { "upang", "para", "ay" },
                new String[] { "upang", "para", "ay",",", "."},
                new String[] { "upang", "para", "ay",",", "."},
                new String[] { "para", "ay", "upang", ",", "."} };
            String[][] enderPOSTypeExclusive = new String[][] { new String[] { "VB" },
                new String[] { "VB" },
                new String[] { "VB"},
                new String[] { "VB"},
                new String[] { "VB"},
                new String[] { "VB" } };
            String[] startMarkersInclusive = new String[] { "kamakalawa",
                "kamakala-wa",
                "ngayong"};
            String[][] endMarkersInclusive = new String[][] { new String[] { "gabi", "umaga", "hapon" },
                new String[] { "gabi", "umaga", "hapon" },
                new String[] { "gabi", "umaga", "hapon" } };
            String[][] enderPOSTypeInclusive = new String[][] { new String[] { "VB" },
                new String[] { "VB" },
                new String[] { "VB" } };
            String[] gazette = new String[] { "kahapon" };
            for (int i = 0; i < tokenizedArticle.Count; i++)
            {
                i = getCandidateByNer("DATE", i, candidates, tokenizedArticle);
                getCandidateByMarkers(generalStopWords, startMarkersExclusive, endMarkersExclusive, null, null, enderPOSTypeExclusive, i, candidates, tokenizedArticle, true);
                getCandidateByMarkers(generalStopWords, startMarkersInclusive, endMarkersInclusive, null, null, enderPOSTypeInclusive, i, candidates, tokenizedArticle, false);
                getCandidateByGazette(gazette, i, candidates, tokenizedArticle);

                if (tokenizedArticle[i].Sentence > 3)
                {
                    break;
                }
            }

            for (int can = 0; can < candidates.Count; can++)
            {
                for (int a = 0; a < can; a++)
                {
                    if (candidates[can].Value != null && candidates[can].Value.Equals(candidates[a].Value))
                    {
                        candidates.RemoveAt(can);
                        if (can > 0)
                        {
                            can--;
                        }
                        break;
                    }
                }
            }

            foreach (var candidate in candidates)
            {
                candidate.Value = candidate.Value.Replace("`` ", "\"");
                candidate.Value = candidate.Value.Replace("``", "\"");
                candidate.Value = candidate.Value.Replace(" ''", "\"");
                candidate.Value = candidate.Value.Replace("''", "\"");
                //System.Console.WriteLine("WHEN CANDIDATE " + candidate.Value);
            }

            return candidates;
        }

        public List<Candidate> performWhereCandidateSelection(List<Token> tokenizedArticle, String articleTitle)
        {
            List<Candidate> candidates = new List<Candidate>();
            String[] generalStopWords = new String[] { "dahil",
                "dahil",
                "dahilan",
                "subalit",
                "makaraang",
                "kaya",
                "kung",
                "kapag",
                "ngunit",
                "palibhassa",
                "sapagkat",
                "sana",
                "kundi",
                "ni",
                "naglalayong",
                "Enero",
                "January",
                "Pebrero",
                "February",
                "Marso",
                "March",
                "April",
                "Abril",
                "Mayo",
                "May",
                "Hunyo",
                "June",
                "Hulyo",
                "July",
                "Agosto",
                "August",
                "Setyembre",
                "September",
                "Oktubre",
                "October",
                "Nobyembre",
                "November",
                "Disyembre",
                "December" };
            String[] startMarkers = new String[] { "ang",
                "nasa",
                "sa" ,
                "ng"};
            String[][] endMarkers = new String[][] { new String[] { "ay"},
                new String[] { "para"},
                new String[] { "mula", "para", "noong", "nuong","sa","kamakalawa","kamakala-wa","kamakailan","kahapon",".","ang","ay"},
                new String[] { "para"} };
            String[][] enderMarkers = new String[][] { new String[] { },
                new String[] { },
                new String[] { "sabado", "hapon","umaga","gabi","miyerkules","lunes","martes","huwebes","linggo","biyernes","alas","oras"},
                new String[] { "sabado", "hapon","umaga","gabi","miyerkules","lunes","martes","huwebes","linggo","biyernes","alas","oras"} };
            String[][] enderPOSType = new String[][] { new String[] { "VB" },
                new String[] { "VB" },
                new String[] { "VB" },
                new String[] { "VB" }};
            for (int i = 0; i < tokenizedArticle.Count; i++)
            {
                getCandidateByMarkers(generalStopWords, startMarkers, endMarkers, enderMarkers, null, enderPOSType, i, candidates, tokenizedArticle, true);
                i = getCandidateByNer("LOC", i, candidates, tokenizedArticle);
                //getCandidateByMarkers(generalStopWords, startMarkers, endMarkers, enderMarkers, null, enderPOSType, i, candidates, tokenizedArticle, true);

                if (tokenizedArticle[i].Sentence > 3)
                {
                    break;
                }
            }

            for (int can = 0; can < candidates.Count; can++)
            {
                for (int a = 0; a < can; a++)
                {
                    if (candidates[can].Value != null && candidates[can].Value.Equals(candidates[a].Value))
                    {
                        candidates.RemoveAt(can);
                        if (can > 0)
                        {
                            can--;
                        }
                        break;
                    }
                }
            }

            foreach (var candidate in candidates)
            {
                candidate.Value = candidate.Value.Replace("`` ", "\"");
                candidate.Value = candidate.Value.Replace("``", "\"");
                candidate.Value = candidate.Value.Replace(" ''", "\"");
                candidate.Value = candidate.Value.Replace("''", "\"");
                //System.Console.WriteLine("WHERE CANDIDATE " + candidate.Value);
            }

            return candidates;
        }

        public List<List<Token>> performWhatCandidateSelection(List<Token> tokenizedArticle, String articleTitle)
        {
            int maxNumberOfCandidates = 3;
            List<List<Token>> candidates = new List<List<Token>>();
            List<List<Token>> segregatedArticle = tokenizedArticle
                .GroupBy(token => token.Sentence)
                .Select(tokenGroup => tokenGroup.ToList())
                .ToList();

            for (int nI = 0; nI < Math.Min(maxNumberOfCandidates, segregatedArticle.Count()); nI++)
            {
                candidates.Add(segregatedArticle[nI]);
            }

            return candidates;
        }

        public List<List<Token>> performWhyCandidateSelection(List<Token> tokenizedArticle, String articleTitle)
        {
            int maxNumberOfCandidates = 4;
            List<List<Token>> candidates = new List<List<Token>>();
            List<List<Token>> segregatedArticle = tokenizedArticle
                .GroupBy(token => token.Sentence)
                .Select(tokenGroup => tokenGroup.ToList())
                .ToList();

            for (int nI = 0; nI < Math.Min(maxNumberOfCandidates, segregatedArticle.Count()); nI++)
            {
                candidates.Add(segregatedArticle[nI]);
            }

            return candidates;
        }

        private int getCandidateByNer(String nerTag, int i, List<Candidate> candidates, List<Token> tokenizedArticle)
        {
            if (tokenizedArticle[i].NamedEntity.Equals(nerTag))
            {
                int startIndex = i;
                String strValue = tokenizedArticle[i].Value;
                int tempWs = tokenizedArticle[i].Frequency;

                while ((i + 1) < tokenizedArticle.Count && tokenizedArticle[i].NamedEntity == tokenizedArticle[i + 1].NamedEntity)
                {
                    i++;
                    if (tokenizedArticle[i].Value.Equals(",") || tokenizedArticle[i].Value.Equals("."))
                    {
                        strValue += tokenizedArticle[i].Value;
                    }
                    else
                    {
                        strValue += " " + tokenizedArticle[i].Value;
                    }
                    if (tokenizedArticle[i].Frequency > tempWs)
                    {
                        tempWs = tokenizedArticle[i].Frequency;
                    }
                }

                int endIndex = i;

                var newToken = new Candidate(strValue, tokenizedArticle[startIndex].Position, tokenizedArticle[endIndex].Position - tokenizedArticle[startIndex].Position);
                newToken.Sentence = tokenizedArticle[i].Sentence; // candidate.token[0].sentence;
                newToken.NamedEntity = tokenizedArticle[i].NamedEntity; // candidate.token[0].NamedEntity;
                newToken.PartOfSpeech = tokenizedArticle[i].PartOfSpeech; // candidate.token[0].NamedEntity;
                newToken.Frequency = tempWs; // candidate.token[0].Frequency;
                candidates.Add(newToken);

                //System.Console.WriteLine("CANDIDATE BY NER [{0}]: {1} (Position {2})", nerTag, newToken.Value, newToken.Position);
            }
            return i;
        }

        private void getCandidateByMarkers(String[] generalStopWords, String[] startMarkers, String[][] endMarkers, String[][] enderMarkers, String[][] enderPOS, String[][] enderPOSType, int i, List<Candidate> candidates, List<Token> tokenizedArticle, Boolean isExclusive)
        {
            /*
            generalStopWords are words that stops a phrase from being a candidate for all start markers
            startMarkers starts the possibility of a phrase from being a candidate
            endMarkers determines the end of the candidate (different per startMarker)
            enderMarkers contains words that stops the phrase from being a candidate if found (different per startMarker)
            enderPOS contains POS that stops the phrase from being a candidate (different per startMarker)
            enderPOSType contains POS Type e.g VB for all verbs that stops the phrase from being a candidate (different per startMarker)
            */
            for (int j = 0; j < startMarkers.Length; j++)
            {
                if (tokenizedArticle[i].Value.Equals(startMarkers[j], StringComparison.OrdinalIgnoreCase))
                {
                    int sentenceNumber = tokenizedArticle[i].Sentence;
                    String strValue = null;
                    String posValue = null;
                    if (!isExclusive)
                    {
                        strValue = tokenizedArticle[i].Value;
                        posValue = tokenizedArticle[i].PartOfSpeech;
                    }
                    int tempWs = 0;
                    Boolean flag = true;
                    Boolean endMarkerFound = false;
                    i++;
                    int startIndex = i;
                    while (flag)
                    {
                        foreach (String markers in endMarkers[j])
                        {
                            if (tokenizedArticle[i].Value.Equals(markers))
                            {
                                endMarkerFound = true;
                                flag = false;
                                break;
                            }
                        }
                        if (generalStopWords != null)
                        {
                            foreach (String stopWords in generalStopWords)
                            {
                                if (tokenizedArticle[i].Value.Equals(stopWords, StringComparison.OrdinalIgnoreCase))
                                {
                                    flag = false;
                                    break;
                                }
                            }
                        }
                        if (enderPOS != null)
                        {
                            foreach (String POS in enderPOS[j])
                            {
                                if (tokenizedArticle[i].PartOfSpeech.Equals(POS, StringComparison.OrdinalIgnoreCase))
                                {
                                    flag = false;
                                    break;
                                }
                            }
                        }
                        if (enderPOSType != null)
                        {
                            if (tokenizedArticle[i].PartOfSpeech == null)
                            {
                                flag = false;
                                break;
                            }                                
                            foreach (String Type in enderPOSType[j])
                            {
                                if (tokenizedArticle[i].PartOfSpeech.StartsWith(Type))
                                {
                                    flag = false;
                                    break;
                                }
                            }
                        }
                        if (enderMarkers != null)
                        {
                            foreach (String markers in enderMarkers[j])
                            {
                                if (tokenizedArticle[i].Value.Equals(markers, StringComparison.OrdinalIgnoreCase))
                                {
                                    flag = false;
                                    break;
                                }
                            }
                        }
                        if (tokenizedArticle[i].Sentence != sentenceNumber)
                        {
                            flag = false;
                        }
                        i++;
                        if (i >= tokenizedArticle.Count)
                        {
                            flag = false;
                        }
                    }

                    int endIndex;
                    if (isExclusive)
                    {
                        endIndex = i - 1;
                    }
                    else
                    {
                        endIndex = i;
                    }
                    if (endMarkerFound)
                    {
                        for (int k = startIndex; k < endIndex; k++)
                        {
                            if (strValue == null)
                            {
                                strValue = tokenizedArticle[k].Value;
                                posValue = tokenizedArticle[k].PartOfSpeech;
                            }
                            else
                            {
                                strValue += (tokenizedArticle[k].Value.Equals(",") || tokenizedArticle[k].Value.Equals(".") ? "" : " ") + tokenizedArticle[k].Value;
                                posValue += " " + tokenizedArticle[k].PartOfSpeech;
                            }

                            if (tokenizedArticle[k].Frequency > tempWs)
                            {
                                tempWs = tokenizedArticle[k].Frequency;
                            }
                        }
                        if (strValue != null) {
                            var newToken = new Candidate(strValue, tokenizedArticle[startIndex].Position, tokenizedArticle[endIndex].Position - tokenizedArticle[startIndex].Position);
                            newToken.Sentence = tokenizedArticle[startIndex].Sentence;
                            newToken.NamedEntity = tokenizedArticle[endIndex].NamedEntity;
                            newToken.PartOfSpeech = tokenizedArticle[endIndex].PartOfSpeech;
                            newToken.Frequency = tempWs;
                            candidates.Add(newToken);
                        }

                        //System.Console.WriteLine("CANDIDATE BY MARKERS: {0}"/*\n\t{1}*/, newToken.Value/*, posValue*/);
                    }
                    else
                    {
                        i = startIndex - 1;
                    }
                    j = startMarkers.Length;
                }
            }
        }

        private int getCandidateByPos(String posTag, int i, List<Candidate> candidates, List<Token> tokenizedArticle)
        {
            if (i < tokenizedArticle.Count && tokenizedArticle[i].PartOfSpeech != null && tokenizedArticle[i].PartOfSpeech.Equals(posTag))
            {
                int startIndex = i;
                String strValue = tokenizedArticle[i].Value;
                int tempWs = tokenizedArticle[i].Frequency;

                while ((i + 1) < tokenizedArticle.Count && tokenizedArticle[i].PartOfSpeech == tokenizedArticle[i + 1].PartOfSpeech)
                {
                    i++;
                    strValue += " " + tokenizedArticle[i].Value;
                    if (tokenizedArticle[i].Frequency > tempWs)
                    {
                        tempWs = tokenizedArticle[i].Frequency;
                    }
                }

                int endIndex = i;

                var newToken = new Candidate(strValue, tokenizedArticle[startIndex].Position, tokenizedArticle[endIndex].Position - tokenizedArticle[startIndex].Position);
                newToken.Sentence = tokenizedArticle[i].Sentence;
                newToken.NamedEntity = tokenizedArticle[i].NamedEntity;
                newToken.PartOfSpeech = tokenizedArticle[i].PartOfSpeech;
                newToken.Frequency = tempWs;
                candidates.Add(newToken);

                //System.Console.WriteLine("CANDIDATE BY POS [{0}]: {1} (Position {2})", posTag, newToken.Value, newToken.Position);
            }
            return i;
        }

        private void getCandidateByGazette(String[] gazette, int i, List<Candidate> candidates, List<Token> tokenizedArticle)
        {
            if (i < tokenizedArticle.Count && tokenizedArticle[i].Sentence <= 3)
            {
                if(gazette.Contains(tokenizedArticle[i].Value))
                {
                    var newToken = new Candidate(tokenizedArticle[i].Value, tokenizedArticle[i].Position, 1);
                    newToken.Sentence = tokenizedArticle[i].Sentence;
                    newToken.NamedEntity = tokenizedArticle[i].NamedEntity;
                    newToken.PartOfSpeech = tokenizedArticle[i].PartOfSpeech;
                    newToken.Frequency = tokenizedArticle[i].Frequency;
                    candidates.Add(newToken);

                    //System.Console.WriteLine("CANDIDATE BY GAZETTER: {0} (Position {1})", newToken.Value, newToken.Position);
                }
            }
        }
    }
}
