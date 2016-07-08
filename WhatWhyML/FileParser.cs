using IE.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace IE
{
    class FileParser
    {
        public List<Article> parseFile(String path)
        {
            List<Article> articleList = new List<Article>();

            try
            {
                String xmlContents = "";
                using (StreamReader streamReader = new StreamReader(path, Encoding.UTF8))
                {
                    xmlContents = streamReader.ReadToEnd();
                }
                xmlContents = WebUtility.HtmlDecode(xmlContents);
                xmlContents = xmlContents.Replace("&", "&amp;");

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlContents);
                XmlNodeList articleNodes = doc.DocumentElement.SelectNodes("/data/article");

                foreach (XmlNode articleNode in articleNodes)
                {
                    Article article = new Article();

                    article.Author = articleNode.SelectSingleNode("author").InnerText;
                    article.Body = WebUtility.HtmlDecode(articleNode.SelectSingleNode("body").InnerText);
                    article.Link = articleNode.SelectSingleNode("link").InnerText;
                    article.Title = WebUtility.HtmlDecode(articleNode.SelectSingleNode("title").InnerText);

                    String date = articleNode.SelectSingleNode("date").SelectSingleNode("month").InnerText + "/" +
                        articleNode.SelectSingleNode("date").SelectSingleNode("day").InnerText + "/" +
                        articleNode.SelectSingleNode("date").SelectSingleNode("year").InnerText;

                    DateTime tempDate = new DateTime(2000, 01, 01);
                    DateTime.TryParse(date, out tempDate);
                    article.Date = tempDate;

                    articleList.Add(article);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while parsing file: " + e);
            }

            return articleList;
        }

        public List<Annotation> parseAnnotations(String path)
        {
            List<Annotation> annotationList = new List<Annotation>();

            String xmlContents = "";
            using (StreamReader streamReader = new StreamReader(path, Encoding.UTF8))
            {
                xmlContents = streamReader.ReadToEnd();
            }
            xmlContents = WebUtility.HtmlDecode(xmlContents);
            xmlContents = xmlContents.Replace("&", "&amp;");

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlContents);

            XmlNodeList articleNodes = doc.DocumentElement.SelectNodes("/data/article");

            foreach (XmlNode articleNode in articleNodes)
            {
                Annotation annotation = new Annotation();

                annotation.Who = WebUtility.HtmlDecode(articleNode.SelectSingleNode("who").InnerText);
                annotation.Where = WebUtility.HtmlDecode(articleNode.SelectSingleNode("where").InnerText);
                annotation.When = WebUtility.HtmlDecode(articleNode.SelectSingleNode("when").InnerText);
                annotation.What = WebUtility.HtmlDecode(articleNode.SelectSingleNode("what").InnerText);
                annotation.Why = WebUtility.HtmlDecode(articleNode.SelectSingleNode("why").InnerText);

                annotationList.Add(annotation);
            }

            return annotationList;
        }
    }
}
