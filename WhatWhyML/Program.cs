using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using IE.Models;
using weka.classifiers;
using weka.core;

namespace IE
{
    class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
#if DEBUG
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Main());
#else
            Boolean isAnnotated = true;
            FileParser fileparserFP = new FileParser();
            String sourcePath = @"..\..\training_news.xml";
            String destinationPath = @"..\..\result.xml";
            String invertedDestinationPath = @"..\..\result_inverted_index.xml";
            String formatDateDestinationPath = @"..\..\result_format_date.xml";

            List<Article> listCurrentArticles = fileparserFP.parseFile(sourcePath);
            List<Annotation> listCurrentTrainingAnnotations = new List<Annotation>();
            if (isAnnotated)
            {
                 listCurrentTrainingAnnotations = fileparserFP.parseAnnotations(sourcePath);
            }
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


            if (listCurrentArticles != null && listCurrentArticles.Count > 0 &&
                (!isAnnotated || (listCurrentTrainingAnnotations != null && listCurrentTrainingAnnotations.Count > 0 &&
                listCurrentArticles.Count == listCurrentTrainingAnnotations.Count)))
            {
                Preprocessor preprocessor = new Preprocessor();

                //Temporarily set to 2 because getting all articles takes longer run time
                for (int nI = 0; nI < listCurrentArticles.Count; nI++)
                {
                    preprocessor.setCurrentArticle(listCurrentArticles[nI]);
                    preprocessor.preprocess();

                    if (isAnnotated)
                    {
                        preprocessor.setCurrentAnnotation(listCurrentTrainingAnnotations[nI]);
                        preprocessor.performAnnotationAssignment();
                    }

                    listTokenizedArticles.Add(preprocessor.getLatestTokenizedArticle());
                    listAllWhoCandidates.Add(preprocessor.getWhoCandidates());
                    listAllWhenCandidates.Add(preprocessor.getWhenCandidates());
                    listAllWhereCandidates.Add(preprocessor.getWhereCandidates());
                    listAllWhatCandidates.Add(preprocessor.getWhatCandidates());
                    listAllWhyCandidates.Add(preprocessor.getWhyCandidates());
                }

                if (isAnnotated)
                {
                    Trainer trainer = new Trainer();
                    trainer.trainMany("who", listTokenizedArticles, listAllWhoCandidates);
                    trainer.trainMany("when", listTokenizedArticles, listAllWhenCandidates);
                    trainer.trainMany("where", listTokenizedArticles, listAllWhereCandidates);
                }
            }

            /*Candidate Selection Printer*/
            /*try
            {
                var whoCandidatesPath = @"..\..\candidates_who.txt";
                var whenCandidatesPath = @"..\..\candidates_when.txt";
                var whereCandidatesPath = @"..\..\candidates_where.txt";

                if (File.Exists(whoCandidatesPath)) File.Delete(whoCandidatesPath);
                if (File.Exists(whenCandidatesPath)) File.Delete(whenCandidatesPath);
                if (File.Exists(whereCandidatesPath)) File.Delete(whereCandidatesPath);

                using (StreamWriter sw = File.CreateText(whoCandidatesPath))
                {
                    for (int nI = 0; nI < listAllWhoCandidates.Count; nI++)
                    {
                        sw.WriteLine("#{0}:", nI);
                        foreach (var candidate in listAllWhoCandidates[nI])
                        {
                            sw.Write(candidate.Value + ", ");
                        }
                        sw.WriteLine("\n");
                    }
                }
                using (StreamWriter sw = File.CreateText(whenCandidatesPath))
                {
                    for (int nI = 0; nI < listAllWhenCandidates.Count; nI++)
                    {
                        sw.WriteLine("#{0}:", nI);
                        foreach (var candidate in listAllWhenCandidates[nI])
                        {
                            sw.Write(candidate.Value + ", ");
                        }
                        sw.WriteLine("\n");
                    }
                }
                using (StreamWriter sw = File.CreateText(whereCandidatesPath))
                {
                    for (int nI = 0; nI < listAllWhereCandidates.Count; nI++)
                    {
                        sw.WriteLine("#{0}:", nI);
                        foreach (var candidate in listAllWhereCandidates[nI])
                        {
                            sw.Write(candidate.Value + ", ");
                        }
                        sw.WriteLine("\n");
                    }
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Error with writing initial line of training dataset.");
            }*/

            Identifier annotationIdentifier = new Identifier();
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

            ResultWriter rw = new ResultWriter(destinationPath, formatDateDestinationPath, invertedDestinationPath, listCurrentArticles, listAllWhoAnnotations, listAllWhenAnnotations, listAllWhereAnnotations, listAllWhatAnnotations, listAllWhyAnnotations);
            rw.generateOutput();
            rw.generateOutputFormatDate();
            rw.generateInvertedIndexOutput();
#endif
        }
    }
}
