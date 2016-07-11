using IE.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace IE
{
    public partial class Main : Form
    {
        private String[] sourcePaths = new String[3];
        private FileParser fileparserFP = new FileParser();

        private List<TextBox> textBoxes = new List<TextBox>();
        private List<GroupBox> firstBoxes = new List<GroupBox>();
        private List<GroupBox> secondBoxes = new List<GroupBox>();
        private List<ComboBox> comboBoxes = new List<ComboBox>();

        private List<Article> listViewerArticles = new List<Article>();
        private List<Article> listNavigatorArticles = new List<Article>();
        private List<Article> listSearchArticles = new List<Article>();

        private List<Annotation> listViewerAnnotations = new List<Annotation>();
        private List<Annotation> listNavigatorAnnotations = new List<Annotation>();
        private List<Annotation> listSearchAnnotations = new List<Annotation>();

        private List<TextBox> searchQueries = new List<TextBox>();
        private List<ComboBox> criteriaBoxes = new List<ComboBox>();
        private List<ComboBox> criteriaTypes = new List<ComboBox>();

        private Dictionary<string, List<int>> whoReverseIndex = new Dictionary<string, List<int>>();
        private Dictionary<string, List<int>> whenReverseIndex = new Dictionary<string, List<int>>();
        private Dictionary<string, List<int>> whereReverseIndex = new Dictionary<string, List<int>>();
        private Dictionary<string, List<int>> whatReverseIndex = new Dictionary<string, List<int>>();
        private Dictionary<string, List<int>> whyReverseIndex = new Dictionary<string, List<int>>();

        private String[] criterias = new String[] { "Sino", "Kailan", "Saan", "Ano", "Bakit" };
        private String[] types = new String[] { "AND", "OR" };

        public Main()
        {
            InitializeComponent();

            textBoxes.Add(textBox1);
            textBoxes.Add(textBox3);
            textBoxes.Add(textBox4);

            firstBoxes.Add(groupBox1);
            firstBoxes.Add(groupBox3);
            firstBoxes.Add(groupBox4);

            secondBoxes.Add(groupBox2);
            secondBoxes.Add(groupBox6);
            secondBoxes.Add(groupBox5);

            comboBoxes.Add(null);
            comboBoxes.Add(comboBox4);
            comboBoxes.Add(comboBox5);

            foreach (String s in criterias)
            {
                criteriaBox.Items.Add(s);
            }

            criteriaBox.SelectedIndex = 0;
            searchQueries.Add(searchQuery);
            criteriaBoxes.Add(criteriaBox);
        }

        private void loadArticles()
        {
            comboBoxes[tabControl1.SelectedIndex].Items.Clear();

            foreach (Article a in tabControl1.SelectedIndex == 1 ?
                listViewerArticles :
                listSearchArticles)
            {
                comboBoxes[tabControl1.SelectedIndex].Items.Add(a.Title);
            }

            if (comboBoxes[tabControl1.SelectedIndex].Items.Count > 0)
            {
                comboBoxes[tabControl1.SelectedIndex].SelectedIndex = 0;
            }
        }

        public void saveChanges(int[] i, Annotation a)
        {
            XmlDocument doc = new XmlDocument();

            doc.Load(sourcePaths[i[0]]);

            XmlNode root = doc.DocumentElement;

            root.SelectSingleNode("/data/article[" + (a.Index + 1) + "]")["who"].InnerText = a.Who;
            root.SelectSingleNode("/data/article[" + (a.Index + 1) + "]")["when"].InnerText = a.When;
            root.SelectSingleNode("/data/article[" + (a.Index + 1) + "]")["where"].InnerText = a.Where;
            root.SelectSingleNode("/data/article[" + (a.Index + 1) + "]")["what"].InnerText = a.What;
            root.SelectSingleNode("/data/article[" + (a.Index + 1) + "]")["why"].InnerText = a.Why;

            doc.Save(sourcePaths[i[0]]);

            if (tabControl1.SelectedIndex == 1)
            {
                listViewerAnnotations[a.Index] = a;
            }
            else if (tabControl1.SelectedIndex == 2)
            {
                listNavigatorAnnotations[a.Index] = a;
            }
        }

        private void btnBrowseImport_Click(object sender, EventArgs e)
        {
            Stream s = null;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Import news articles (*.xml)";
            ofd.Filter = "XML files|*.xml";
            ofd.InitialDirectory = @"C:\";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if ((s = ofd.OpenFile()) != null)
                    {
                        using (s)
                        {
                            textBoxes[tabControl1.SelectedIndex].Text = ofd.FileName;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            if (!textBoxes[tabControl1.SelectedIndex].Text.Equals(""))
            {
                FileInfo fi = new FileInfo(textBoxes[tabControl1.SelectedIndex].Text);


                if (File.Exists(fi.FullName) && fi.Extension.Equals(".xml"))
                {
                    sourcePaths[tabControl1.SelectedIndex] = fi.FullName;

                    if (tabControl1.SelectedIndex > 0)
                    {
                        List<Article> listArticles = fileparserFP.parseFile(sourcePaths[tabControl1.SelectedIndex]);
                        List<Annotation> listAnnotations = fileparserFP.parseAnnotations(sourcePaths[tabControl1.SelectedIndex]);

                        if (listArticles.Count <= 0)
                        {
                            MessageBox.Show("No articles found!");
                            return;
                        }

                        foreach (int i in Enumerable.Range(0, listAnnotations.Count()))
                        {
                            listAnnotations[i].Index = i;
                            Console.WriteLine(listArticles[i].Title + " " + i);
                        }

                        if (tabControl1.SelectedIndex == 1)
                        {
                            listViewerArticles = listArticles;
                            listViewerAnnotations = listAnnotations;

                            loadArticles();
                        }
                        else if (tabControl1.SelectedIndex == 2)
                        {
                            String formatDateDestinationPath = fi.FullName.Insert(fi.FullName.Length - 4, "_inverted_index");

                            if (File.Exists(formatDateDestinationPath))
                            {
                                listNavigatorArticles = listArticles;
                                listNavigatorAnnotations = listAnnotations;

                                XmlDocument doc = new XmlDocument();

                                doc.Load(formatDateDestinationPath);

                                XmlNodeList whoNodes = doc.DocumentElement.SelectNodes("/data/who/entry");
                                XmlNodeList whenNodes = doc.DocumentElement.SelectNodes("/data/when/entry");
                                XmlNodeList whereNodes = doc.DocumentElement.SelectNodes("/data/where/entry");
                                XmlNodeList whatNodes = doc.DocumentElement.SelectNodes("/data/what/entry");
                                XmlNodeList whyNodes = doc.DocumentElement.SelectNodes("/data/why/entry");

                                foreach (XmlNode entry in whoNodes)
                                {
                                    List<int> indices = new List<int>();
                                    foreach (XmlNode index in entry.SelectNodes("articleIndex"))
                                    {
                                        indices.Add(Convert.ToInt32(index.InnerText));
                                    }
                                    whoReverseIndex.Add(entry["text"].InnerText, indices);
                                }

                                foreach (XmlNode entry in whenNodes)
                                {
                                    List<int> indices = new List<int>();
                                    foreach (XmlNode index in entry.SelectNodes("articleIndex"))
                                    {
                                        indices.Add(Convert.ToInt32(index.InnerText));
                                    }
                                    whenReverseIndex.Add(entry.SelectSingleNode("text").InnerText, indices);
                                }

                                foreach (XmlNode entry in whereNodes)
                                {
                                    List<int> indices = new List<int>();
                                    foreach (XmlNode index in entry.SelectNodes("articleIndex"))
                                    {
                                        indices.Add(Convert.ToInt32(index.InnerText));
                                    }
                                    whereReverseIndex.Add(entry.SelectSingleNode("text").InnerText, indices);
                                }

                                foreach (XmlNode entry in whatNodes)
                                {
                                    List<int> indices = new List<int>();
                                    foreach (XmlNode index in entry.SelectNodes("articleIndex"))
                                    {
                                        indices.Add(Convert.ToInt32(index.InnerText));
                                    }
                                    whatReverseIndex.Add(entry.SelectSingleNode("text").InnerText, indices);
                                }

                                foreach (XmlNode entry in whyNodes)
                                {
                                    List<int> indices = new List<int>();
                                    foreach (XmlNode index in entry.SelectNodes("articleIndex"))
                                    {
                                        indices.Add(Convert.ToInt32(index.InnerText));
                                    }
                                    whyReverseIndex.Add(entry.SelectSingleNode("text").InnerText, indices);
                                }
                            }
                            else
                            {
                                MessageBox.Show("Inverted index file not found!");
                                return;
                            }
                        }
                    }

                    //firstBoxes[tabControl1.SelectedIndex].Enabled = false;
                    secondBoxes[tabControl1.SelectedIndex].Enabled = true;
                }
            }
        }

        private void btnView_Click(object sender, EventArgs e)
        {
            ArticleView view = new ArticleView(this,
                new int[]
                {
                    tabControl1.SelectedIndex,
                    comboBoxes[tabControl1.SelectedIndex].SelectedIndex
                },
                (tabControl1.SelectedIndex == 1 ?
                listViewerArticles :
                listSearchArticles)[comboBoxes[tabControl1.SelectedIndex].SelectedIndex],
                (tabControl1.SelectedIndex == 1 ?
                listViewerAnnotations :
                listSearchAnnotations)[comboBoxes[tabControl1.SelectedIndex].SelectedIndex]);

            view.ShowDialog();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            if (tabControl1.SelectedIndex == 1)
            {
                resetViewer();
            }
            else if (tabControl1.SelectedIndex == 2)
            {
                resetNavigator();
            }
        }

        #region Extractor Methods

        private Boolean extract(String destinationPath, String invertedDestinationPath, String formatDateDestinationPath)
        {
            List<Article> listCurrentArticles = fileparserFP.parseFile(sourcePaths[tabControl1.SelectedIndex]);
            List<List<Token>> listTokenizedArticles = new List<List<Token>>();
            List<List<Candidate>> listAllWhoCandidates = new List<List<Candidate>>();
            List<List<Candidate>> listAllWhenCandidates = new List<List<Candidate>>();
            List<List<Candidate>> listAllWhereCandidates = new List<List<Candidate>>();
            List<List<List<Token>>> listAllWhatCandidates = new List<List<List<Token>>>();
            List<List<List<Token>>> listAllWhyCandidates = new List<List<List<Token>>>();
            List<List<String>> listAllWhoAnnotations = new List<List<String>>();
            List<List<String>> listAllWhenAnnotations = new List<List<String>>();
            List<List<String>> listAllWhereAnnotations = new List<List<String>>();
            List<String> listAllWhatAnnotations = new List<String>();
            List<String> listAllWhyAnnotations = new List<String>();

            //List<Annotation> listCurrentTrainingAnnotations = new List<Annotation>();

            //listCurrentTrainingAnnotations = fileparserFP.parseAnnotations(sourcePaths[tabControl1.SelectedIndex]);

            if (listCurrentArticles != null && listCurrentArticles.Count > 0)
            {
                Preprocessor preprocessor = new Preprocessor();
                float precisionWho = 0;
                float recallWho = 0;
                float precisionWhen = 0;
                float recallWhen = 0;
                float precisionWhere = 0;
                float recallWhere = 0;
                float precisionWhat = 0;
                float recallWhat = 0;
                float precisionWhy = 0;
                float recallWhy = 0;
                float totalWho = 0;
                float totalWhen = 0;
                float totalWhere = 0;
                float totalWhat = 0;
                float totalWhy = 0;
                float sentenceZeroWhat = 0;
                float sentenceOneWhat = 0;
                float sentenceTwoWhat = 0;
                float sentenceThreeWhat = 0;
                float sentenceFourWhat = 0;
                float sentenceFiveWhat = 0;
                float sentenceZeroWhy = 0;
                float sentenceOneWhy = 0;
                float sentenceTwoWhy = 0;
                float sentenceThreeWhy = 0;
                float sentenceFourWhy = 0;
                float sentenceFiveWhy = 0;

                //Temporarily set to 2 because getting all articles takes longer run time
                for (int nI = 0; nI < listCurrentArticles.Count; nI++)
                {
                    float[][] statistics;
                    preprocessor.setCurrentArticle(listCurrentArticles[nI]);
                    preprocessor.preprocess();

                    listTokenizedArticles.Add(preprocessor.getLatestTokenizedArticle());
                    listAllWhoCandidates.Add(preprocessor.getWhoCandidates());
                    listAllWhenCandidates.Add(preprocessor.getWhenCandidates());
                    listAllWhereCandidates.Add(preprocessor.getWhereCandidates());
                    listAllWhatCandidates.Add(preprocessor.getWhatCandidates());
                    listAllWhyCandidates.Add(preprocessor.getWhyCandidates());

                    /*preprocessor.setCurrentAnnotation(listCurrentTrainingAnnotations[nI]);
                    statistics = preprocessor.performAnnotationAssignment();

                    if (statistics != null)
                    {
                        recallWho += statistics[0][0];
                        recallWhen += statistics[1][0];
                        recallWhere += statistics[2][0];
                        recallWhat += statistics[3][0];
                        recallWhy += statistics[4][0];
                        precisionWho += statistics[0][1];
                        precisionWhen += statistics[1][1];
                        precisionWhere += statistics[2][1];
                        precisionWhat += statistics[3][1];
                        precisionWhy += statistics[4][1];
                        totalWho += statistics[0][2];
                        totalWhen += statistics[1][2];
                        totalWhere += statistics[2][2];
                        totalWhat += statistics[3][2];
                        totalWhy += statistics[4][2];
                        int sentenceNumber = (int)statistics[3][3];
                        switch (sentenceNumber)
                        {
                            case -1:
                                break;
                            case 0:
                                sentenceZeroWhat += 1;
                                break;
                            case 1:
                                sentenceOneWhat += 1;
                                break;
                            case 2:
                                sentenceTwoWhat += 1;
                                break;
                            case 3:
                                sentenceThreeWhat += 1;
                                break;
                            case 4:
                                sentenceFourWhat += 1;
                                break;
                            case 5:
                                sentenceFiveWhat += 1;
                                break;
                            default:
                                sentenceFiveWhat += 1;
                                break;
                        }
                        sentenceNumber = (int)statistics[4][3];
                        switch (sentenceNumber)
                        {
                            case -1:
                                break;
                            case 0:
                                sentenceZeroWhy += 1;
                                break;
                            case 1:
                                sentenceOneWhy += 1;
                                break;
                            case 2:
                                sentenceTwoWhy += 1;
                                break;
                            case 3:
                                sentenceThreeWhy += 1;
                                break;
                            case 4:
                                sentenceFourWhy += 1;
                                break;
                            case 5:
                                sentenceFiveWhy += 1;
                                break;
                            default:
                                sentenceFiveWhy += 1;
                                break;
                        }
                    }

                    System.Console.WriteLine("Article #{0}", nI + 1);
                    System.Console.WriteLine("Recall Who: " + statistics[0][0]);
                    System.Console.WriteLine("Recall When: " + statistics[1][0]);
                    System.Console.WriteLine("Recall Where: " + statistics[2][0]);
                    System.Console.WriteLine("Recall What: " + statistics[3][0]);
                    System.Console.WriteLine("Recall Why: " + statistics[4][0]);
                    System.Console.WriteLine("Precision Who: " + statistics[0][1]);
                    System.Console.WriteLine("Precision When: " + statistics[1][1]);
                    System.Console.WriteLine("Precision Where: " + statistics[2][1]);
                    System.Console.WriteLine("Precision What: " + statistics[3][1]);
                    System.Console.WriteLine("Precision Why: " + statistics[4][1]);*/
                }

                //System.Console.WriteLine("Average Statistics");
                //System.Console.WriteLine("Recall Who: " + recallWho / totalWho);
                //System.Console.WriteLine("Recall When: " + recallWhen / totalWhen);
                //System.Console.WriteLine("Recall Where: " + recallWhere / totalWhere);
                //System.Console.WriteLine("Recall What: " + recallWhat / totalWhat);
                //System.Console.WriteLine("Recall Why: " + recallWhy / totalWhy);
                //System.Console.WriteLine("Precision Who: " + precisionWho / totalWho);
                //System.Console.WriteLine("Precision When: " + precisionWhen / totalWhere);
                //System.Console.WriteLine("Precision Where: " + precisionWhere / totalWhen);
                //System.Console.WriteLine("Precision What: " + precisionWhat / totalWhat);
                //System.Console.WriteLine("Precision Why: " + precisionWhy / totalWhy);
                //System.Console.WriteLine("What sentence location :");
                //System.Console.WriteLine("Sentence 0: " + sentenceZeroWhat + " Percentage: " + sentenceZeroWhat/ totalWhat);
                //System.Console.WriteLine("Sentence 1: " + sentenceOneWhat + " Percentage: " + sentenceOneWhat / totalWhat);
                //System.Console.WriteLine("Sentence 2: " + sentenceTwoWhat + " Percentage: " + sentenceTwoWhat / totalWhat);
                //System.Console.WriteLine("Sentence 3: " + sentenceThreeWhat + " Percentage: " + sentenceThreeWhat / totalWhat);
                //System.Console.WriteLine("Sentence 4: " + sentenceFourWhat + " Percentage: " + sentenceFourWhat / totalWhat);
                //System.Console.WriteLine("Sentence >= 5: " + sentenceFiveWhat + " Percentage: " + sentenceFiveWhat / totalWhat);
                //System.Console.WriteLine("Why sentence location :");
                //System.Console.WriteLine("Sentence 0: " + sentenceZeroWhy + " Percentage: " + sentenceZeroWhy / totalWhy);
                //System.Console.WriteLine("Sentence 1: " + sentenceOneWhy + " Percentage: " + sentenceOneWhy / totalWhy);
                //System.Console.WriteLine("Sentence 2: " + sentenceTwoWhy + " Percentage: " + sentenceTwoWhy / totalWhy);
                //System.Console.WriteLine("Sentence 3: " + sentenceThreeWhy + " Percentage: " + sentenceThreeWhy / totalWhy);
                //System.Console.WriteLine("Sentence 4: " + sentenceFourWhy + " Percentage: " + sentenceFourWhy / totalWhy);
                //System.Console.WriteLine("Sentence >= 5: " + sentenceFiveWhy + " Percentage: " + sentenceFiveWhy / totalWhy);
            }
            else
            {
                MessageBox.Show("Invalid XML File!");
                return false;
            }

            Identifier annotationIdentifier = new Identifier(false, null);
            for (int nI = 0; nI < listCurrentArticles.Count; nI++)
            {
                annotationIdentifier.setCurrentArticle(listTokenizedArticles[nI]);
                annotationIdentifier.setWhoCandidates(listAllWhoCandidates[nI]);
                annotationIdentifier.setWhenCandidates(listAllWhenCandidates[nI]);
                annotationIdentifier.setWhereCandidates(listAllWhereCandidates[nI]);
                annotationIdentifier.setWhatCandidates(listAllWhatCandidates[nI]);
                annotationIdentifier.setWhyCandidates(listAllWhyCandidates[nI]);
                annotationIdentifier.setTitle(listCurrentArticles[nI].Title);
                annotationIdentifier.labelAnnotations();
                listAllWhoAnnotations.Add(annotationIdentifier.getWho());
                listAllWhenAnnotations.Add(annotationIdentifier.getWhen());
                listAllWhereAnnotations.Add(annotationIdentifier.getWhere());
                listAllWhatAnnotations.Add(annotationIdentifier.getWhat());
                listAllWhyAnnotations.Add(annotationIdentifier.getWhy());
            }

            ResultWriter rw = new ResultWriter(destinationPath, invertedDestinationPath, formatDateDestinationPath, listCurrentArticles, listAllWhoAnnotations, listAllWhenAnnotations, listAllWhereAnnotations, listAllWhatAnnotations, listAllWhyAnnotations);
            rw.generateOutput();
            rw.generateOutputFormatDate();
            rw.generateInvertedIndexOutput();

            return true;
        }

        private void btnBrowseExtract_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Extract 5W's to file (*.xml)";
            sfd.Filter = "XML files|*.xml";
            sfd.InitialDirectory = @"C:\";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                textBox2.Text = sfd.FileName;
            }
        }

        private void btnExtract_Click(object sender, EventArgs e)
        {
            FileInfo fi;

            if (!String.IsNullOrWhiteSpace(textBox2.Text))
            {
                fi = new FileInfo(textBox2.Text);

                if (fi.Extension.Equals(".xml"))
                {
                    String destinationPath = fi.FullName;
                    String invertedDestinationPath = fi.FullName.Insert(fi.FullName.Length - 4, "_inverted_index");
                    String formatDateDestinationPath = fi.FullName.Insert(fi.FullName.Length - 4, "_format_date");

                    Boolean success = extract(destinationPath, invertedDestinationPath, formatDateDestinationPath);
                    if (success)
                    {
                        MessageBox.Show("Operation completed successfully!");
                    }
                    resetExtractor();

                    textBox3.Text = destinationPath;
                    textBox4.Text = destinationPath;
                }
            }
        }

        private void resetExtractor()
        {
            groupBox1.Enabled = true;
            groupBox2.Enabled = false;
            textBox1.Text = "";
            textBox2.Text = "";
        }

        #endregion

        #region Viewer Methods

        private void resetViewer()
        {
            groupBox3.Enabled = true;
            groupBox6.Enabled = false;
            textBox3.Text = "";
            comboBox4.Items.Clear();
            listViewerArticles = null;
            listViewerAnnotations = null;
        }

        #endregion

        #region Navigator Methods

        private void btnSearch_Click(object sender, EventArgs e)
        {
            //Initialize variables
            listSearchArticles.Clear();
            listSearchAnnotations.Clear();
            groupBox7.Enabled = true;

            List<int> whoIndex = new List<int>();
            List<int> whenIndex = new List<int>();
            List<int> whereIndex = new List<int>();
            List<int> whatIndex = new List<int>();
            List<int> whyIndex = new List<int>();
            List<int> finalResults = new List<int>();
            List<int>[] queryResults = new List<int>[searchQueries.Count]; // result of each query
            for (int i = 0; i < searchQueries.Count; i++)
            {
                queryResults[i] = new List<int>();
            }
            List<List<int>> mergedAndResults = new List<List<int>>();
            Console.WriteLine("im in");

            //Find the index of the queries for each w
            for (int i = 0; i < criteriaBoxes.Count; i++)
            {
                switch (criteriaBoxes[i].Text)
                {
                    case "Sino":
                        whoIndex.Add(i);
                        break;
                    case "Kailan":
                        whenIndex.Add(i);
                        break;
                    case "Saan":
                        whereIndex.Add(i);
                        break;
                    case "Ano":
                        whatIndex.Add(i);
                        break;
                    case "Bakit":
                        whyIndex.Add(i);
                        break;
                    default:
                        break;
                }
            }

            //Find matches for who
            if (whoIndex.Count > 0)
            {
                Console.WriteLine("im in");
                Console.WriteLine(whoReverseIndex.Count);
                foreach (KeyValuePair<String, List<int>> entry in whoReverseIndex)
                {
                    foreach (int queryIndex in whoIndex)
                    {
                        Console.WriteLine(entry.Key + " VS" + searchQueries[queryIndex].Text);
                        if (entry.Key.IndexOf(searchQueries[queryIndex].Text, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            Console.WriteLine("im in 2");
                            queryResults[queryIndex].AddRange(entry.Value);
                        }
                    }
                }
            }

            //Find matches for when
            if (whenIndex.Count > 0)
            {
                foreach (KeyValuePair<String, List<int>> entry in whenReverseIndex)
                {
                    foreach (int queryIndex in whenIndex)
                    {
                        if (entry.Key.IndexOf(searchQueries[queryIndex].Text, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            queryResults[queryIndex].AddRange(entry.Value);
                        }
                    }
                }
            }

            //Find matches for where
            if (whereIndex.Count > 0)
            {
                foreach (KeyValuePair<String, List<int>> entry in whereReverseIndex)
                {
                    foreach (int queryIndex in whereIndex)
                    {
                        if (entry.Key.IndexOf(searchQueries[queryIndex].Text, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            queryResults[queryIndex].AddRange(entry.Value);
                        }
                    }
                }
            }

            //Find matches for what
            if (whatIndex.Count > 0)
            {
                foreach (KeyValuePair<String, List<int>> entry in whatReverseIndex)
                {
                    foreach (int queryIndex in whatIndex)
                    {
                        if (entry.Key.IndexOf(searchQueries[queryIndex].Text, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            queryResults[queryIndex].AddRange(entry.Value);
                        }
                    }
                }
            }

            //Find matches for why
            if (whyIndex.Count > 0)
            {
                foreach (KeyValuePair<String, List<int>> entry in whyReverseIndex)
                {
                    foreach (int queryIndex in whyIndex)
                    {
                        if (entry.Key.IndexOf(searchQueries[queryIndex].Text, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            queryResults[queryIndex].AddRange(entry.Value);
                        }
                    }
                }
            }


            //Merge results from queries
            for (int i = 0; i < criteriaTypes.Count; i++)
            {
                if (criteriaTypes[i].Text.Equals("AND"))
                {
                    queryResults[i + 1] = queryResults[i].Intersect<int>(queryResults[i + 1]).ToList<int>();
                }
                else if (criteriaTypes[i].Text.Equals("OR"))
                {
                    mergedAndResults.Add(queryResults[i + 1]);
                }
            }

            if (mergedAndResults.Count > 0)
            {
                foreach (List<int> result in mergedAndResults)
                {
                    finalResults.AddRange(result);
                }
                finalResults = finalResults.Distinct().ToList();
            }
            else
            {
                finalResults = queryResults[queryResults.Length - 1].Distinct().ToList();
            }

            foreach (int index in finalResults)
            {
                listSearchArticles.Add(listNavigatorArticles[index]);
                listSearchAnnotations.Add(listNavigatorAnnotations[index]);
            }

            if (finalResults.Count() > 0)
            {
                btnNavigatorView.Enabled = true;
            }
            else
            {
                btnNavigatorView.Enabled = false;
            }

            loadArticles();
        }

        private void btnAddCriteria_Click(object sender, EventArgs e)
        {
            TextBox newQuery = new TextBox();

            newQuery.Name = "searchQuery" + searchQueries.Count;
            newQuery.Location = new Point(181,
                searchQueries.Count == 0 ?
                29 :
                (searchQueries[searchQueries.Count - 1].Location.Y + 25));
            newQuery.Width = 319;
            newQuery.Visible = true;

            searchQueries.Add(newQuery);
            panel1.Controls.Add(newQuery);

            ComboBox newCriteria = new ComboBox();

            newCriteria.Name = "criteriaBox" + criteriaBoxes.Count;
            newCriteria.Location = new Point(3,
                criteriaBoxes.Count == 0 ?
                29 :
                (criteriaBoxes[criteriaBoxes.Count - 1].Location.Y + 25));
            newCriteria.Width = 91;
            newCriteria.Visible = true;
            newCriteria.DropDownStyle = ComboBoxStyle.DropDownList;

            foreach (String s in criterias)
            {
                newCriteria.Items.Add(s);
            }

            newCriteria.SelectedIndex = 0;

            criteriaBoxes.Add(newCriteria);
            panel1.Controls.Add(newCriteria);

            ComboBox newType = new ComboBox();

            newType.Name = "criteriaType" + criteriaTypes.Count;
            newType.Location = new Point(100,
                criteriaTypes.Count == 0 ?
                29 :
                (criteriaTypes[criteriaTypes.Count - 1].Location.Y + 25));
            newType.Width = 75;
            newType.Visible = true;
            newType.DropDownStyle = ComboBoxStyle.DropDownList;

            foreach (String s in types)
            {
                newType.Items.Add(s);
            }

            newType.SelectedIndex = 0;

            criteriaTypes.Add(newType);
            panel1.Controls.Add(newType);
        }

        private void resetNavigator()
        {
            groupBox4.Enabled = true;
            groupBox5.Enabled = false;
            groupBox7.Enabled = false;
            textBox4.Text = "";
            searchQuery.Text = "";
            comboBox5.Items.Clear();
            listNavigatorArticles = null;
            listNavigatorAnnotations = null;

            criteriaBox.SelectedIndex = 0;

            foreach (TextBox t in searchQueries)
            {
                if (t != searchQuery)
                {
                    panel1.Controls.Remove(t);
                }
            }

            foreach (ComboBox c in criteriaBoxes)
            {
                if (c != criteriaBox)
                {
                    panel1.Controls.Remove(c);
                }
            }

            foreach (ComboBox c in criteriaTypes)
            {
                panel1.Controls.Remove(c);
            }

            searchQueries.Clear();
            criteriaBoxes.Clear();
            criteriaTypes.Clear();
            listSearchArticles.Clear();
            listSearchAnnotations.Clear();
            searchQueries.Add(searchQuery);
            criteriaBoxes.Add(criteriaBox);

            whoReverseIndex.Clear();
            whenReverseIndex.Clear();
            whereReverseIndex.Clear();
            whatReverseIndex.Clear();
            whyReverseIndex.Clear();
        }

        #endregion
    }
}
