using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Imaging;
using iTextSharp;
using iTextSharp.text.pdf;

namespace stockAnalyzer
{

    /*
     * The main list containing companies and their values
     */
    class stockList
    {
        List<headNode> list;

        public stockList()
        {
            list = new List<headNode>();
        }

        public string toString()
        {
            string returnable = "";

            foreach (headNode h in list)
            {
                returnable += h.toString();
            }

            return returnable;
        }

        public void addToList(headNode hn)
        {
            if (hn == null)
            {
                Console.WriteLine("headNode you tried to add to list was null");
                return;
            }
            list.Add(hn);
        }

        public headNode getRightHead(String name)
        {
            foreach (headNode hn in list)
            {
                if (hn.getName().Equals(name))
                {
                    return hn;
                }
            }

            headNode h = new headNode(name);
            addToList(h);

            return h;
        }

        public List<headNode> getList()
        {
            return list;
        }
    }

    /*
     * The first node in a list containing different stockvalues from different dates
     */
    class headNode
    {
        String name;
        List<stockNode> list;

        public headNode(String name)
        {
            this.name = name;
            list = new List<stockNode>();
        }

        public string toString()
        {
            string returnable = "";
            foreach (stockNode n in list)
            {
                returnable += n.toString() + "|";
            }
            returnable += "#";
            return returnable;
        }

        public List<stockNode> getList()
        {
            return list;
        }

        public void addNode(stockNode n)
        {
            if (n == null)
            {
                Console.WriteLine("stockNode you tried to add to list was null");
                return;
            }
            list.Add(n);
        }

        public String getName()
        {
            return name;
        }
    }

    /*
     * Basic node for a stock object containing name of the company, date and stockvalue on that date
     */
    class stockNode
    {
        String name;
        double value;
        DateTime date;

        public stockNode(String name, double value)
        {
            this.name = name;
            this.value = value;
            //this.date = new DateTime();
            this.date = DateTime.Today;
        }

        public stockNode(String name, double value, DateTime date)
        {
            this.name = name;
            this.value = value;
            this.date = date;
        }

        public string toString()
        {
            return name + "!" + value + "!" + date;
        }

        public String getName()
        {
            return name;
        }

        public double getValue()
        {
            return value;
        }

        public DateTime getDate()
        {
            return date;
        }
    }

    /*
     * Handles extraction of the webpage and initial formatting
     */
    class WebScraper
    {
        public WebScraper()
        {
        }

        public List<string> listFromFile()
        {
            string line;
            List<string> l = new List<string>();
            Boolean alku = false;

            using (StreamReader reader = new StreamReader(@"sivu.txt", Encoding.Default))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    if (alku == false && line.Contains("<th class=\"lcol ra\"><a class=\"listheaderlink\" href=\"/arvopaperi/site/list.page?magic=(cc (page finshares) (submenu all) (list (sort (col JavaTm) (dir DESC))))\">Aika</a>"))
                    {
                        l.Add(line);
                        alku = true;
                    }
                    else if (alku == true)
                    {
                        l.Add(line);
                        if (line.Contains("<a href=\"http://www.six-telekurs.fi\">Tiedon esitt"))
                        {
                            break;
                        }
                    }
                }
            }
            return l;
        }
        /*
         * Downloads stock listings in html
         */
        public void dlSite()
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.GetEncoding("UTF-8");
            wc.DownloadFile("http://porssi.talouselama.fi/arvopaperi/site/list.page?magic=(cc%20(page%20finshares)%20(submenu%20all))", "sivu.txt");

        }

    }

    /*
     * Handles management of data, formatting, saving and loading.
     */
    class DataManager
    {

        public DataManager()
        {
        }

        /*
         * Parses downloaded stock html into more manageable form
         */
        public List<string> cleanList(List<string> l)
        {

            List<string> cleanedList = new List<string>();
            bool skip = false;
            int count = 1;

            // regexp pattern to remove all tags
            var pattern = @"(?!</?b>)<.*?>";
            string rubbish = "Tiedon esittäjä ja koostaja on SIX Telekurs.Tiedot esitetään 15 minuutin viiveellä.";
            string rubbish2 = "Aika";

            //Fileprint just for dev purposes and testing
            //using (StreamWriter w = new StreamWriter("cleaned.txt"))
            //{
            foreach (string s in l)
            {
                string s1 = Regex.Replace(s, pattern, String.Empty, RegexOptions.Multiline);
                s1 = s1.Trim();

                if (s1 != "" && s1 != rubbish && s1 != rubbish2)
                {
                    if (s1.Contains("Muutos") && !skip)
                    {
                        count = 1;
                        skip = true;
                        continue;
                    }
                    if (skip)
                    {
                        count++;
                        if (count == 14)
                        {
                            skip = !skip;
                        }
                        continue;
                    }

                    if (!skip)
                    {
                        //Save to file for inspection
                        //w.WriteLine(s1);
                        //Console.WriteLine(s1);
                        cleanedList.Add(s1);
                    }
                }
            }
            //}
            return cleanedList;
        }

        /*
         * Method for updating datastructure or creating new one. Uses loadList() to fetch any previous 
         * datastructure for appending.
         */
        public stockList constructAndUpdateDataStructure(List<String> list)
        {
            //change so that first checks if previous list exists
            stockList sl = loadList();

            ////only used for testing purposes
            //using (StreamWriter w = new StreamWriter("onlyNameAndValue.txt"))
            //{
            Boolean found = false;
            int count = 1;
            headNode h = null;

            foreach (string s in list)
            {
                if (s.Contains("&nbsp;") && !found)
                {
                    count = 1;
                    found = true;
                    continue;
                }
                if (found)
                {
                    //Save to file for inspection
                    //w.WriteLine(s);
                    //Console.WriteLine(s1);

                    if (count == 1)
                    {
                        h = sl.getRightHead(s);
                    }
                    else if (count == 2)
                    {
                        if (s == "-")
                        {
                            double val = 0.0;
                            String name = h.getName();
                            stockNode sn = new stockNode(name, val);
                            h.addNode(sn);
                        }
                        else
                        {
                            double val = Convert.ToDouble(s);
                            String name = h.getName();
                            stockNode sn = new stockNode(name, val);
                            h.addNode(sn);
                        }
                    }
                    else if (count == 3)
                    {
                        found = !found;
                    }
                    count++;
                    continue;
                }
            }
            //}

            return sl;
        }

        /*
         * Saves list using StreamWriter for later use
         */
        public void saveList(stockList sl)
        {
            using (StreamWriter w = new StreamWriter("stock_data.txt"))
            {
                w.WriteLine(sl.toString());
            }
        }

        /*
         * Loads list from memory and parses it back into list form
         */
        public stockList loadList()
        {
            //check if stock_data exists, if not then this is first time running and return new 
            if (!File.Exists("stock_data.txt"))
            {
                return new stockList();
            }

            stockList sl = new stockList();

            //Read stock_data using Streamreader and parse it into format that can be handled
            using (StreamReader reader = new StreamReader(@"stock_data.txt", Encoding.UTF8))
            {
                String line = reader.ReadToEnd();

                String[] headNodes = line.Split(new char[] { '#' });

                for (int i = 0; i < headNodes.Count(); i++)
                {
                    String[] stockNodes = headNodes[i].Split(new char[] { '|' });

                    for (int j = 0; j < stockNodes.Count(); j++)
                    {
                        if (stockNodes[j] == "")
                        {
                            continue;
                        }

                        String[] data = stockNodes[j].Split(new char[] { '!' });

                        if (data.Count() < 3)
                        {
                            continue;
                        }

                        String s = data[2].Split(' ').First();
                        String[] date = s.Split(new char[] { '.' });
                        String name = data[0];

                        int day = Convert.ToInt32(date[0]);
                        int month = Convert.ToInt32(date[1]);
                        int year = Convert.ToInt32(date[2]);

                        double val = Convert.ToDouble(data[1]);
                        DateTime d = new DateTime(year, month, day);
                        stockNode sn = new stockNode(name, val, d);
                        sl.getRightHead(name).addNode(sn);
                    }
                }
                return sl;
            }
        }
    }

    class stockAnalyzer
    {
        public stockAnalyzer()
        {
        }

        /*
         * Create a chart from given headNode
         */
        public Image createChart(headNode hn)
        {
            Image img = new Bitmap(800, 500);
            Graphics g = Graphics.FromImage(img);
            g.Clear(Color.White);
            Rectangle r = new Rectangle(49, 50, 700, 340);
            g.DrawRectangle(Pens.Black, r);

            // Draws lines for chart
            for (int i = 1; i < 17; i++)
            {
                g.DrawLine(Pens.LightGray, 50, (20 * i) + 50, 748, (20 * i) + 50);
            }

            int size = hn.getList().Count();
            int start;
            int end = size;
            Point[] points;

            if (size < 20)
            {
                start = 0;
                points = new Point[size];
            }
            else
            {
                start = size - 21;
                points = new Point[20];
            }

            int multiplier = 10;

            //defines points with values
            for (int i = 0; i < points.Count(); i++)
            {
                //if stockprice is high will try not to draw them too up
                if (i == 0)
                {
                    if (hn.getList()[0].getValue() > 30 || hn.getList()[size - 1].getValue() > 30)
                    {
                        multiplier = 1;
                    }
                }

                //If value is zero then chart would look funny because of sudden drop, so we can try to get previous value and hope
                //it is larger than zero, because zero mean the value has not changed
                if (Convert.ToInt32(hn.getList()[i + start].getValue()) == 0)
                {
                    //If i+start is 0 then material used is less than 20 so it is impossible to get any previous value
                    if (i + start == 0)
                    {
                        int val = (390 - ((Convert.ToInt32(hn.getList()[0].getValue() * multiplier))));
                        g.DrawString(hn.getList()[0].getValue().ToString(), new Font("Tahoma", 8), Brushes.Black, new RectangleF(50 + (35 * i), val - 20, 130 + (35 * i), 430));
                        points[i] = new Point((i * 35) + 50, val);
                    }
                    //And we then know that start is greater than zero so we can get atleast previous value
                    else
                    {
                        int val = (390 - ((Convert.ToInt32(hn.getList()[(i - 1) + start].getValue() * multiplier))));
                        g.DrawString(hn.getList()[(i - 1) + start].getValue().ToString(), new Font("Tahoma", 8), Brushes.Black, new RectangleF(50 + (35 * i), val - 20, 130 + (35 * i), 430));
                        points[i] = new Point((i * 35) + 50, val);
                    }

                }
                //If value is not zero then we can use current value
                else
                {
                    int val = (390 - ((Convert.ToInt32(hn.getList()[i + start].getValue() * multiplier))));
                    g.DrawString(hn.getList()[i + start].getValue().ToString(), new Font("Tahoma", 8), Brushes.Black, new RectangleF(50 + (35 * i), val - 20, 130 + (35 * i), 430));
                    points[i] = new Point((i * 35) + 50, val);
                }

            }

            //Draws dates used for points
            for (int i = start; i < end; i++)
            {
                g.DrawString(hn.getList()[i].getDate().ToString("dd.hh"), new Font("Tahoma", 8), Brushes.Black, new RectangleF(50 + (35 * i), 410, 130 + (35 * i), 430));
            }

            g.DrawString("0", new Font("Tahoma", 8), Brushes.Black, new RectangleF(20, 380, 80, 390));

            if (points.Count() > 1)
            {
                g.DrawCurve(Pens.Black, points);
            }


            //img.Save(hn.getName() + "_chart.jpg");
            return img;
        }

        /*
         * Creates the first (index) page of the pdf document
         */
        public void createFirstPage(stockList sl, iTextSharp.text.Document doc, PdfWriter w)
        {
            String titleString = "Helsingin pörssi " + sl.getList().First().getList()[0].getDate().ToShortDateString();
            iTextSharp.text.Paragraph p = new iTextSharp.text.Paragraph(titleString);
            doc.Add(p);

            String frontPage = "";

            //Build the initial showable string from different nodes
            foreach (headNode hn in sl.getList())
            {
                int count = 1;
                stockNode n = hn.getList()[hn.getList().Count() - count];
                if (n.getValue() == 0)
                {
                    while (n.getValue() == 0 && count != hn.getList().Count())
                    {
                        count++;
                        n = hn.getList()[hn.getList().Count - count];
                    }
                }
                int length = n.getValue().ToString().Length;
                frontPage += n.getValue().ToString().PadRight(15 - length);
                frontPage += "";
                frontPage += hn.getName();
                frontPage += "\n";
            }
            //Console.WriteLine(frontPage);

            String[] companies = frontPage.Split(new String[] { "\n" }, StringSplitOptions.None);
            iTextSharp.text.pdf.ColumnText col = new iTextSharp.text.pdf.ColumnText(w.DirectContent);
            col.Alignment = iTextSharp.text.Element.ALIGN_JUSTIFIED;
            iTextSharp.text.Paragraph frontPageParag = new iTextSharp.text.Paragraph(frontPage);

            //Put the strings in colStrings that fit the page nicely
            for (int i = 0; i < sl.getList().Count(); i++)
            {
                String colString = "";
                colString += companies[i];
                colString += "\n";
                col.AddElement(new iTextSharp.text.Paragraph(colString));
                colString = "";
            }

            int status = 0;
            int loop = 0;
            //Build the actual columns into pdf
            while (ColumnText.HasMoreText(status))
            {
                if (loop % 2 != 0)
                {
                    col.SetSimpleColumn((doc.PageSize.Width / 2) + 5, 72, (doc.PageSize.Width) - 72, doc.PageSize.Height - 72);

                }
                else
                {
                    col.SetSimpleColumn(72, 72, (doc.PageSize.Width / 2) - 5, doc.PageSize.Height - 72);
                    doc.NewPage();
                }
                loop++;
                status = col.Go();
            }
        }

        /*
         * Creates the pdf from data
         */
        public void createPDF(stockList sl)
        {
            iTextSharp.text.Document doc = new iTextSharp.text.Document(iTextSharp.text.PageSize.A4);

            PdfWriter w = PdfWriter.GetInstance(doc, new FileStream("stock.pdf", FileMode.Create));
            w.CloseStream = false;
            doc.Open();

            //Add current stock values on the first page along with link to page with detailed info
            doc.NewPage();

            //Construct the first (index) page with all companies with their current stocks
            createFirstPage(sl, doc, w);

            //Construct page for each company and a graph for the company
            foreach (headNode hn in sl.getList())
            {
                doc.NewPage();

                Image image = createChart(hn);
                iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(image, System.Drawing.Imaging.ImageFormat.Gif);
                img.ScalePercent(60f);
                
                iTextSharp.text.Paragraph name = new iTextSharp.text.Paragraph(hn.getName());
                
                doc.Add(new iTextSharp.text.Paragraph(name));

                doc.Add(img);

            }
            
            doc.Close();
        }
    }

    class Program
    {

        static void Main(string[] args)
        {
            //Main program's required components
            WebScraper ws = new WebScraper();
            DataManager dm = new DataManager();
            stockAnalyzer sa = new stockAnalyzer();

            ws.dlSite();
            List<String> l = ws.listFromFile();
            List<String> l1 = dm.cleanList(l);

            stockList sl = dm.constructAndUpdateDataStructure(l1);
            dm.saveList(sl);

            //For testing main program without downloading new content
            //stockList sl = dm.loadList();


            //not necessarily needed but useful lines for testing
            //////Console.WriteLine(sl.toString());

            //////using (StreamWriter w = new StreamWriter("stock_data2.txt"))
            //////{
            //////    w.WriteLine(sl.toString());
            //////}

            //////sa.createChart(sl.getList()[0]);
            //////sa.createChart(sl.getList()[1]);
            //////sa.createChart(sl.getList()[2]);
            //////sa.createChart(sl.getList()[4]);

            //creation of the analysis and visualization
            sa.createPDF(sl);

            //to prevent quit exit and allow to see Console prints
            Console.ReadLine();
        }
    }
}
