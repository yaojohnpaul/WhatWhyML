using IE.Models;
using java.io;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using weka.classifiers;
using weka.classifiers.meta;
using weka.core;
using weka.filters;
using weka.filters.unsupervised.attribute;

namespace IE
{
    public class Identifier
    {
        private List<Token> articleCurrent;
        private String titleCurrent;
        private List<List<Token>> segregatedArticleCurrent;
        private List<Candidate> listWhoCandidates;
        private List<Candidate> listWhenCandidates;
        private List<Candidate> listWhereCandidates;
        private List<List<Token>> listWhatCandidates;
        private List<List<Token>> listWhyCandidates;
        private List<String> listWho;
        private List<String> listWhen;
        private List<String> listWhere;
        private String strWhat;
        private String strWhy;
        private FastVector fvPOS;
        Classifier whoClassifier;
        Classifier whenClassifier;
        Classifier whereClassifier;

        public Identifier()
        {
            listWhoCandidates = new List<Candidate>();
            listWhenCandidates = new List<Candidate>();
            listWhereCandidates = new List<Candidate>();
            listWhatCandidates = new List<List<Token>>();
            listWhyCandidates = new List<List<Token>>();

            fvPOS = new FastVector(Token.PartOfSpeechTags.Length);
            foreach (String POS in Token.PartOfSpeechTags)
            {
                fvPOS.addElement(POS);
            }

            whoClassifier = (Classifier)SerializationHelper.read(@"..\..\IdentifierModels\who.model");
            whenClassifier = (Classifier)SerializationHelper.read(@"..\..\IdentifierModels\when.model");
            whereClassifier = (Classifier)SerializationHelper.read(@"..\..\IdentifierModels\where.model");

            initializeAnnotations();
        }

        private void initializeAnnotations()
        {
            listWho = new List<String>();
            listWhen = new List<String>();
            listWhere = new List<String>();
            strWhat = "";
            strWhy = "";
        }

        #region Setters
        public void setCurrentArticle(List<Token> pArticle)
        {
            articleCurrent = pArticle;
            segregatedArticleCurrent = articleCurrent
                        .GroupBy(token => token.Sentence)
                        .Select(tokenGroup => tokenGroup.ToList())
                        .ToList();
        }

        public void setWhoCandidates(List<Candidate> pCandidates)
        {
            listWhoCandidates = pCandidates;
        }

        public void setWhenCandidates(List<Candidate> pCandidates)
        {
            listWhenCandidates = pCandidates;
        }

        public void setWhereCandidates(List<Candidate> pCandidates)
        {
            listWhereCandidates = pCandidates;
        }

        public void setWhatCandidates(List<List<Token>> pCandidates)
        {
            listWhatCandidates = pCandidates;
        }

        public void setWhyCandidates(List<List<Token>> pCandidates)
        {
            listWhyCandidates = pCandidates;
        }

        public void setTitle(String pTitle)
        {
            titleCurrent = pTitle;
        }
        #endregion

        #region Getters
        public List<Token> getCurrentArticle()
        {
            return articleCurrent;
        }

        public List<String> getWho()
        {
            return listWho;
        }

        public List<String> getWhen()
        {
            return listWhen;
        }

        public List<String> getWhere()
        {
            return listWhere;
        }

        public String getWhat()
        {
            return strWhat;
        }

        public String getWhy()
        {
            return strWhy;
        }
        #endregion

        public void labelAnnotations()
        {
            initializeAnnotations();
            labelWho();
            labelWhen();
            labelWhere();
            labelWhat();
            labelWhy();
        }

        #region Labelling Functions
        private void labelWho()
        {
            Instances whoInstances = createWhoInstances();

            /*for (int i = 0; i < whoInstances.size(); i++)
            {
                double[] classProbability = whoClassifier.distributionForInstance(whoInstances.get(i));
                if (classProbability[0] >= classProbability[1])
                {
                    listWho.Add(listWhoCandidates[i].Value == null ? "" : listWhoCandidates[i].Value);
                }
            }*/
            foreach (Instance instance in whoInstances)
            {
                double[] classProbability = whoClassifier.distributionForInstance(instance);
                if (classProbability[0] >= classProbability[1])
                {
                    listWho.Add(instance.stringValue(0));
                }
            }
        }

        private void labelWhen()
        {
            Instances whenInstances = createWhenInstances();

            foreach (Instance instance in whenInstances)
            {
                double[] classProbability = whenClassifier.distributionForInstance(instance);
                if (classProbability[0] >= classProbability[1])
                {
                    listWhen.Add(instance.stringValue(0));
                }
            }
        }

        private void labelWhere()
        {
            Instances whereInstances = createWhereInstances();

            foreach (Instance instance in whereInstances)
            {
                double[] classProbability = whereClassifier.distributionForInstance(instance);
                if (classProbability[0] >= classProbability[1])
                {
                    listWhere.Add(instance.stringValue(0));
                }
            }
        }

        private void labelWhat()
        {
            double WEIGHT_PER_WHO = 0.3;
            double WEIGHT_PER_WHEN = 0.2;
            double WEIGHT_PER_WHERE = 0.2;
            double WEIGHT_PER_SENTENCE = 0.2;
            double WEIGHT_PER_W_IN_TITLE = 0.1;

            List<double> candidateWeights = new List<double>();
            double highestWeight = -1;

            String[][] markers = new String[][] {
                new String[] { "kaya", "START" },
                new String[] { "para", "END" },
                new String[] { "dahil", "END" },
                new String[] { "upang", "END" },
                new String[] { "makaraang", "END" },
            };

            if (listWhatCandidates.Count > 0)
            {
                foreach (List<Token> candidate in listWhatCandidates)
                {
                    String tempWhat = "";
                    double tempWeight = 0;
                    String[] match;
                    bool hasMarker = false;

                    tempWhat = String.Join(" ", candidate.Select(token => token.Value).ToArray());
                    tempWhat = tempWhat.Replace("-LRB- ", "(");
                    tempWhat = tempWhat.Replace(" -RRB-", ")");
                    tempWhat = tempWhat.Replace(" . ", ".");
                    tempWhat = tempWhat.Replace(" .", ".");
                    tempWhat = tempWhat.Replace(" ,", ",");
                    tempWhat = tempWhat.Replace(" !", "!");

                    tempWeight += listWho.Where(tempWhat.Contains).Count() * WEIGHT_PER_WHO;
                    tempWeight += listWhen.Where(tempWhat.Contains).Count() * WEIGHT_PER_WHEN;
                    tempWeight += listWhere.Where(tempWhat.Contains).Count() * WEIGHT_PER_WHERE;
                    tempWeight += 1 - WEIGHT_PER_SENTENCE * candidate[0].Sentence;

                    tempWeight += listWho.Where(titleCurrent.Contains).Count() * WEIGHT_PER_W_IN_TITLE;
                    tempWeight += listWhen.Where(titleCurrent.Contains).Count() * WEIGHT_PER_W_IN_TITLE;
                    tempWeight += listWhere.Where(titleCurrent.Contains).Count() * WEIGHT_PER_W_IN_TITLE;

                    candidateWeights.Add(tempWeight);
                    //System.Console.WriteLine("---------");
                    //System.Console.WriteLine("Candidate: \t{0}\nWeight: \t{1}", tempWhat, tempWeight);

                    match = markers.FirstOrDefault(s => tempWhat.Contains(s[0]));

                    if (match != null)
                    {
                        tempWhat = (match[1].Equals("START")) ?
                            tempWhat.Substring(tempWhat.IndexOf(match[0]) + match[0].Count() + 1) :
                            tempWhat.Substring(0, tempWhat.IndexOf(match[0]));
                        hasMarker = true;
                    }

                    if (tempWeight > highestWeight)
                    {
                        strWhat = tempWhat;
                        highestWeight = tempWeight;
                    }
                }
            }

            //System.Console.WriteLine("---------");
            //System.Console.WriteLine("WHAT: {0}",
              //  strWhat);
        }

        private void labelWhy()
        {
            double WEIGHT_PER_MARKER = 0.5;
            //double WEIGHT_PER_VERB_MARKER = 0.2;
            double WEIGHT_PER_WHAT = 0.5;
            //double WEIGHT_PER_CHAR = 0.01;
            //double WEIGHT_PER_SENTENCE = 0;
            double CARRY_OVER = 0;

            String[][] markers = new String[][] {
                new String[] { " sanhi sa ", "START" },
                new String[] { " sanhi ng ", "START" },
                new String[] { " sapagkat ", "START" },
                new String[] { " palibhasa ay ", "START" },
                new String[] { " palibhasa ", "START" },
                new String[] { " kasi ", "START" },
                new String[] { " mangyari'y ", "START" },
                new String[] { " mangyari ay ", "START" },
                new String[] { " dahil sa ", "START" },
                new String[] { " dahil na rin sa ", "START" },
                new String[] { " dahil ", "START" },
                new String[] { " dahilan sa", "START" },
                new String[] { " dahilan ", "START" },
                new String[] { " para ", "START" },
                new String[] { " upang ", "START" },
                new String[] { " makaraang ", "START" },
                new String[] { " naglalayong ", "START" },
                new String[] { " kaya ", "END" }
            };

            string[] endMarkers = new string[]
            {
                " makaraang ",
                ", ",
            };

            String[] verbMarkers = new String[]
            {
                "pag-usapan",
                "sinabi",
                "pinalalayo",
                "itatatag",
                "sinisi",
                "nakipag-ugnayan",
                "nagsampa",
                "hiniling"
            };

            List<double> candidateWeights = new List<double>();
            double highestWeight = 0.5;

            if (listWhyCandidates.Count > 0)
            {
                foreach (List<Token> candidate in listWhyCandidates)
                {
                    String tempWhy = "";
                    double tempWeight = 0;
                    String[] match;
                    bool hasWhat = false;
                    bool hasMarker = false;

                    tempWhy = String.Join(" ", candidate.Select(token => token.Value).ToArray());
                    tempWhy = tempWhy.Replace("-LRB- ", "(");
                    tempWhy = tempWhy.Replace(" -RRB-", ")");
                    tempWhy = tempWhy.Replace(" . ", ".");
                    tempWhy = tempWhy.Replace(" .", ".");
                    tempWhy = tempWhy.Replace(" ,", ",");
                    tempWhy = tempWhy.Replace(" !", "!");

                    if (tempWhy.Contains(strWhat))
                    {
                        tempWeight += WEIGHT_PER_WHAT;
                        hasWhat = true;
                    }

                    match = markers.FirstOrDefault(s => tempWhy.Contains(s[0]));

                    if (match != null)
                    {
                        tempWhy = (match[1].Equals("START")) ?
                            tempWhy.Substring(tempWhy.IndexOf(match[0]) + match[0].Count()) :
                            tempWhy.Substring(0, tempWhy.IndexOf(match[0]));
                        tempWeight += WEIGHT_PER_MARKER;
                        hasMarker = true;

                        if (match[1].Equals("START"))
                        {
                            string endMatch = endMarkers.FirstOrDefault(s => tempWhy.Contains(s));

                            if (endMatch != null)
                            {
                                tempWhy = tempWhy.Substring(0, tempWhy.IndexOf(endMatch));
                            }
                        }
                    }

                    tempWeight += CARRY_OVER;
                    CARRY_OVER = 0;

                    if (strWhat.Contains(tempWhy))
                    {
                        tempWeight = 0;
                    }

                    //if(verbMarkers.Any(s => strWhat.ToLower().Contains(s)))
                    //{
                    //    tempWeight += WEIGHT_PER_VERB_MARKER;
                    //}

                    if (strWhat.Equals(tempWhy))
                    {
                        CARRY_OVER = 0.5;
                    }

                    /*System.Console.WriteLine("---------");
                    System.Console.WriteLine("Candidate: \t{0}\nMarker: \t{1}\nWeight: \t{2}", 
                        tempWhy, 
                        match != null ? match[0] : "N/A", 
                        tempWeight);
                    */
                    candidateWeights.Add(tempWeight);

                    if (tempWeight > highestWeight)
                    {
                        strWhy = tempWhy;
                        highestWeight = tempWeight;
                    }
                }
            }
            /*
            System.Console.WriteLine("---------");
            System.Console.WriteLine("WHY: {0}", 
                strWhy);
            */
        }
        #endregion

        #region Instances Creation
        #region Instance Group Creation
        private Instances createWhoInstances()
        {
            FastVector fvWho = createWhoFastVector();
            Instances whoInstances = new Instances("WhoInstances", fvWho, listWhoCandidates.Count);
            foreach (Token candidate in listWhoCandidates)
            {
                if (candidate.Value == null) continue;
                Instance whoInstance = createSingleWhoInstance(fvWho, candidate);
                whoInstance.setDataset(whoInstances);
                whoInstances.add(whoInstance);
            }
            whoInstances.setClassIndex(fvWho.size() - 1);
            return whoInstances;
        }

        private Instances createWhenInstances()
        {
            FastVector fvWhen = createWhenFastVector();
            Instances whenInstances = new Instances("WhenInstances", fvWhen, listWhenCandidates.Count);
            foreach (Token candidate in listWhenCandidates)
            {
                if (candidate.Value == null) continue;
                Instance whenInstance = createSingleWhenInstance(fvWhen, candidate);
                whenInstance.setDataset(whenInstances);
                whenInstances.add(whenInstance);
            }
            whenInstances.setClassIndex(fvWhen.size() - 1);
            return whenInstances;
        }

        private Instances createWhereInstances()
        {
            FastVector fvWhere = createWhereFastVector();
            Instances whereInstances = new Instances("WhereInstances", fvWhere, listWhereCandidates.Count);
            foreach (Token candidate in listWhereCandidates)
            {
                if (candidate.Value == null) continue;
                Instance whereInstance = createSingleWhereInstance(fvWhere, candidate);
                whereInstance.setDataset(whereInstances);
                whereInstances.add(whereInstance);
            }
            whereInstances.setClassIndex(fvWhere.size() - 1);
            return whereInstances;
        }
        #endregion

        private const int whoWordsBefore = 10;
        private const int whoWordsAfter = 10;
        private const int whenWordsBefore = 3;
        private const int whenWordsAfter = 3;
        private const int whereWordsBefore = 10;
        private const int whereWordsAfter = 10;

        #region Single Instance Creation
        private Instance createSingleWhoInstance(FastVector fvWho, Token candidate)
        {
            //first word-n attribute number
            int wordsBeforeFirstAttributeNumber = 6;
            //first pos-n attribute number
            int posBeforeFirstAttributeNumber = wordsBeforeFirstAttributeNumber + whoWordsBefore + whoWordsAfter;
            //word+1 attribute number
            int wordsAfterFirstAttributeNumber = wordsBeforeFirstAttributeNumber + whoWordsBefore;
            //pos+1 attribute number
            int posAfterFirstAttributeNumber = posBeforeFirstAttributeNumber + whoWordsBefore;

            int totalAttributeCount = wordsBeforeFirstAttributeNumber + whoWordsBefore * 2 + whoWordsAfter * 2 + 1;

            Instance whoCandidate = new DenseInstance(totalAttributeCount);
            whoCandidate.setValue((weka.core.Attribute)fvWho.elementAt(0), candidate.Value);
            whoCandidate.setValue((weka.core.Attribute)fvWho.elementAt(1), candidate.Value.Split(' ').Count());
            whoCandidate.setValue((weka.core.Attribute)fvWho.elementAt(2), candidate.Sentence);
            whoCandidate.setValue((weka.core.Attribute)fvWho.elementAt(3), candidate.Position);
            double sentenceStartProximity = -1;
            foreach (List<Token> tokenList in segregatedArticleCurrent)
            {
                if (tokenList.Count > 0 && tokenList[0].Sentence == candidate.Sentence)
                {
                    sentenceStartProximity = (double)(candidate.Position - tokenList[0].Position) / (double)tokenList.Count;
                    break;
                }
            }
            if (sentenceStartProximity > -1)
            {
                whoCandidate.setValue((weka.core.Attribute)fvWho.elementAt(4), sentenceStartProximity);
            }
            whoCandidate.setValue((weka.core.Attribute)fvWho.elementAt(5), candidate.Frequency);

            for (int i = whoWordsBefore; i > 0; i--)
            {
                if (candidate.Position - i - 1 >= 0)
                {
                    whoCandidate.setValue((weka.core.Attribute)fvWho.elementAt(whoWordsBefore - i + wordsBeforeFirstAttributeNumber), articleCurrent[candidate.Position - i - 1].Value);
                    if (articleCurrent[candidate.Position - i - 1].PartOfSpeech != null)
                    {
                        whoCandidate.setValue((weka.core.Attribute)fvWho.elementAt(whoWordsBefore - i + posBeforeFirstAttributeNumber), articleCurrent[candidate.Position - i - 1].PartOfSpeech);
                    }
                }
            }
            for (int i = 0; i < whoWordsAfter; i++)
            {
                if (candidate.Position + i < articleCurrent.Count)
                {
                    whoCandidate.setValue((weka.core.Attribute)fvWho.elementAt(wordsAfterFirstAttributeNumber + i), articleCurrent[candidate.Position + i].Value);
                    if (articleCurrent[candidate.Position + i].PartOfSpeech != null)
                    {
                        whoCandidate.setValue((weka.core.Attribute)fvWho.elementAt(posAfterFirstAttributeNumber + i), articleCurrent[candidate.Position + i].PartOfSpeech);
                    }
                }
            }
            return whoCandidate;
        }

        private Instance createSingleWhenInstance(FastVector fvWhen, Token candidate)
        {
            //first word-n attribute number
            int wordsBeforeFirstAttributeNumber = 4;
            //first pos-n attribute number
            int posBeforeFirstAttributeNumber = wordsBeforeFirstAttributeNumber + whenWordsBefore + whenWordsAfter;
            //word+1 attribute number
            int wordsAfterFirstAttributeNumber = wordsBeforeFirstAttributeNumber + whenWordsBefore;
            //pos+1 attribute number
            int posAfterFirstAttributeNumber = posBeforeFirstAttributeNumber + whenWordsBefore;

            int totalAttributeCount = wordsBeforeFirstAttributeNumber + whenWordsBefore * 2 + whenWordsAfter * 2 + 1;

            Instance whenCandidate = new DenseInstance(totalAttributeCount);
            whenCandidate.setValue((weka.core.Attribute)fvWhen.elementAt(0), candidate.Value);
            whenCandidate.setValue((weka.core.Attribute)fvWhen.elementAt(1), candidate.Value.Split(' ').Count());
            whenCandidate.setValue((weka.core.Attribute)fvWhen.elementAt(2), candidate.Sentence);
            //whenCandidate.setValue((weka.core.Attribute)fvWhen.elementAt(3), candidate.Position);
            //double sentenceStartProximity = -1;
            //foreach (List<Token> tokenList in segregatedArticleCurrent)
            //{
            //    if (tokenList.Count > 0 && tokenList[0].Sentence == candidate.Sentence)
            //    {
            //        sentenceStartProximity = (double)(candidate.Position - tokenList[0].Position) / (double)tokenList.Count;
            //        break;
            //    }
            //}
            //if (sentenceStartProximity > -1)
            //{
            //    whenCandidate.setValue((weka.core.Attribute)fvWhen.elementAt(4), sentenceStartProximity);
            //}
            whenCandidate.setValue((weka.core.Attribute)fvWhen.elementAt(3), candidate.Frequency);
            for (int i = whenWordsBefore; i > 0; i--)
            {
                if (candidate.Position - i - 1 >= 0)
                {
                    whenCandidate.setValue((weka.core.Attribute)fvWhen.elementAt(whenWordsBefore - i + wordsBeforeFirstAttributeNumber), articleCurrent[candidate.Position - i - 1].Value);
                    if (articleCurrent[candidate.Position - i - 1].PartOfSpeech != null)
                    {
                        whenCandidate.setValue((weka.core.Attribute)fvWhen.elementAt(whenWordsBefore - i + posBeforeFirstAttributeNumber), articleCurrent[candidate.Position - i - 1].PartOfSpeech);
                    }
                }
            }
            for (int i = 0; i < whenWordsAfter; i++)
            {
                if (candidate.Position + i < articleCurrent.Count)
                {
                    whenCandidate.setValue((weka.core.Attribute)fvWhen.elementAt(wordsAfterFirstAttributeNumber + i), articleCurrent[candidate.Position + i].Value);
                    if (articleCurrent[candidate.Position + i].PartOfSpeech != null)
                    {
                        whenCandidate.setValue((weka.core.Attribute)fvWhen.elementAt(posAfterFirstAttributeNumber + i), articleCurrent[candidate.Position + i].PartOfSpeech);
                    }
                }
            }
            return whenCandidate;
        }

        private Instance createSingleWhereInstance(FastVector fvWhere, Token candidate)
        {
            //first word-n attribute number
            int wordsBeforeFirstAttributeNumber = 4;
            //first pos-n attribute number
            int posBeforeFirstAttributeNumber = wordsBeforeFirstAttributeNumber + whereWordsBefore + whereWordsAfter;
            //word+1 attribute number
            int wordsAfterFirstAttributeNumber = wordsBeforeFirstAttributeNumber + whereWordsBefore;
            //pos+1 attribute number
            int posAfterFirstAttributeNumber = posBeforeFirstAttributeNumber + whereWordsBefore;

            int totalAttributeCount = wordsBeforeFirstAttributeNumber + whereWordsBefore * 2 + whereWordsAfter * 2 + 1;

            Instance whereCandidate = new DenseInstance(totalAttributeCount);
            whereCandidate.setValue((weka.core.Attribute)fvWhere.elementAt(0), candidate.Value);
            whereCandidate.setValue((weka.core.Attribute)fvWhere.elementAt(1), candidate.Value.Split(' ').Count());
            whereCandidate.setValue((weka.core.Attribute)fvWhere.elementAt(2), candidate.Sentence);
            //whereCandidate.setValue((weka.core.Attribute)fvWhere.elementAt(3), candidate.Position);
            //double sentenceStartProximity = -1;
            //foreach (List<Token> tokenList in segregatedArticleCurrent)
            //{
            //    if (tokenList.Count > 0 && tokenList[0].Sentence == candidate.Sentence)
            //    {
            //        sentenceStartProximity = (double)(candidate.Position - tokenList[0].Position) / (double)tokenList.Count;
            //        break;
            //    }
            //}
            //if (sentenceStartProximity > -1)
            //{
            //    whereCandidate.setValue((weka.core.Attribute)fvWhere.elementAt(4), sentenceStartProximity);
            //}
            whereCandidate.setValue((weka.core.Attribute)fvWhere.elementAt(3), candidate.Frequency);
            for (int i = whereWordsBefore; i > 0; i--)
            {
                if (candidate.Position - i - 1 >= 0)
                {
                    whereCandidate.setValue((weka.core.Attribute)fvWhere.elementAt(whereWordsBefore - i + wordsBeforeFirstAttributeNumber), articleCurrent[candidate.Position - i - 1].Value);
                    if (articleCurrent[candidate.Position - i - 1].PartOfSpeech != null)
                    {
                        whereCandidate.setValue((weka.core.Attribute)fvWhere.elementAt(whereWordsBefore - i + posBeforeFirstAttributeNumber), articleCurrent[candidate.Position - i - 1].PartOfSpeech);
                    }
                }
            }
            for (int i = 0; i < whereWordsAfter; i++)
            {
                if (candidate.Position + i < articleCurrent.Count)
                {
                    whereCandidate.setValue((weka.core.Attribute)fvWhere.elementAt(wordsAfterFirstAttributeNumber + i), articleCurrent[candidate.Position + i].Value);
                    if (articleCurrent[candidate.Position + i].PartOfSpeech != null)
                    {
                        whereCandidate.setValue((weka.core.Attribute)fvWhere.elementAt(posAfterFirstAttributeNumber + i), articleCurrent[candidate.Position + i].PartOfSpeech);
                    }
                }
            }
            return whereCandidate;
        }
        #endregion
        #endregion

        #region Fast Vector Creation
        private FastVector createWhoFastVector()
        {
            FastVector fvWho = new FastVector(7 + whoWordsBefore * 2 + whoWordsAfter * 2);
            fvWho.addElement(new weka.core.Attribute("word", (FastVector)null));
            fvWho.addElement(new weka.core.Attribute("wordCount"));
            fvWho.addElement(new weka.core.Attribute("sentence"));
            fvWho.addElement(new weka.core.Attribute("position"));
            fvWho.addElement(new weka.core.Attribute("sentenceStartProximity"));
            fvWho.addElement(new weka.core.Attribute("wordScore"));
            for (int i = whoWordsBefore; i > 0; i--)
            {
                fvWho.addElement(new weka.core.Attribute("word-" + i, (FastVector)null));
            }
            for (int i = 1; i <= whoWordsAfter; i++)
            {
                fvWho.addElement(new weka.core.Attribute("word+" + i, (FastVector)null));
            }
            for (int i = whoWordsBefore; i > 0; i--)
            {
                fvWho.addElement(new weka.core.Attribute("postag-" + i, fvPOS));
            }
            for (int i = 1; i <= whoWordsAfter; i++)
            {
                fvWho.addElement(new weka.core.Attribute("postag+" + i, fvPOS));
            }
            FastVector fvClass = new FastVector(2);
            fvClass.addElement("yes");
            fvClass.addElement("no");
            fvWho.addElement(new weka.core.Attribute("who", fvClass));
            return fvWho;
        }

        private FastVector createWhenFastVector()
        {
            FastVector fvWhen = new FastVector(5 + whenWordsBefore * 2 + whenWordsAfter * 2);
            fvWhen.addElement(new weka.core.Attribute("word", (FastVector)null));
            fvWhen.addElement(new weka.core.Attribute("wordCount"));
            fvWhen.addElement(new weka.core.Attribute("sentence"));
            //fvWhen.addElement(new weka.core.Attribute("position"));
            //fvWhen.addElement(new weka.core.Attribute("sentenceStartProximity"));
            fvWhen.addElement(new weka.core.Attribute("wordScore"));
            for (int i = whenWordsBefore; i > 0; i--)
            {
                fvWhen.addElement(new weka.core.Attribute("word-" + i, (FastVector)null));
            }
            for (int i = 1; i <= whenWordsAfter; i++)
            {
                fvWhen.addElement(new weka.core.Attribute("word+" + i, (FastVector)null));
            }
            for (int i = whenWordsBefore; i > 0; i--)
            {
                fvWhen.addElement(new weka.core.Attribute("postag-" + i, fvPOS));
            }
            for (int i = 1; i <= whenWordsAfter; i++)
            {
                fvWhen.addElement(new weka.core.Attribute("postag+" + i, fvPOS));
            }
            FastVector fvClass = new FastVector(2);
            fvClass.addElement("yes");
            fvClass.addElement("no");
            fvWhen.addElement(new weka.core.Attribute("when", fvClass));
            return fvWhen;
        }

        private FastVector createWhereFastVector()
        {
            FastVector fvWhere = new FastVector(5 + whereWordsBefore * 2 + whereWordsAfter * 2);
            fvWhere.addElement(new weka.core.Attribute("word", (FastVector)null));
            fvWhere.addElement(new weka.core.Attribute("wordCount"));
            fvWhere.addElement(new weka.core.Attribute("sentence"));
            //fvWhere.addElement(new weka.core.Attribute("position"));
            //fvWhere.addElement(new weka.core.Attribute("sentenceStartProximity"));
            fvWhere.addElement(new weka.core.Attribute("wordScore"));
            for (int i = whereWordsBefore; i > 0; i--)
            {
                fvWhere.addElement(new weka.core.Attribute("word-" + i, (FastVector)null));
            }
            for (int i = 1; i <= whereWordsAfter; i++)
            {
                fvWhere.addElement(new weka.core.Attribute("word+" + i, (FastVector)null));
            }
            for (int i = whereWordsBefore; i > 0; i--)
            {
                fvWhere.addElement(new weka.core.Attribute("postag-" + i, fvPOS));
            }
            for (int i = 1; i <= whereWordsAfter; i++)
            {
                fvWhere.addElement(new weka.core.Attribute("postag+" + i, fvPOS));
            }
            FastVector fvClass = new FastVector(2);
            fvClass.addElement("yes");
            fvClass.addElement("no");
            fvWhere.addElement(new weka.core.Attribute("where", fvClass));
            return fvWhere;
        }
        #endregion
    }
}
