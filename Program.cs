using System.Diagnostics.CodeAnalysis;
using Tesseract;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace nocr
{
    internal class Program
    {
        static void Main(string[] args)
        {
            OcrCNIC ocr = new OcrCNIC(true);
            
            var result = new List<dynamic>();

            //result.AddRange(ocr.CnicFront(new string[] { "images\\manzoor.bmp" }));
            result.AddRange(ocr.CnicFront(new string[] { "images\\adeel.bmp", "images\\amir.bmp", "images\\rafique.bmp", "images\\ehtasham.bmp", "images\\manzoor.bmp" }));
            //result.AddRange(ocr.CnicBack(new string[] { "sample1.back.cnic.jpg", "sample2.back.cnic.jpg" }));
            //result.AddRange(ocr.NicopFront(new string[] { "sample1.back.cnic.jpg", "sample2.back.cnic.jpg" }));
            //result.AddRange(ocr.NicopBack(new string[] { "sample1.back.cnic.jpg", "sample2.back.cnic.jpg" }));
            //result.AddRange(ocr.OldFront(new string[] { "sample1.back.cnic.jpg", "sample2.back.cnic.jpg" }));
            //result.AddRange(ocr.OldFront(new string[] { "sample1.back.cnic.jpg", "sample2.back.cnic.jpg" }));

            foreach (var item in result)
                Console.WriteLine("* {0,5} | x:{1,5} y:{2,5} w:{3,5} h:{4,5} | Mean confidence: {5,8} | Text: {6,50} | File: {7}", item.field, item.x, item.y, item.w, item.h, item.confidence, item.text, item.fileName);
        }

        class OcrCNIC
        {
            void DrawRact(string filename, int x, int y, int h, int w)
            {

                System.Drawing.Image image = System.Drawing.Image.FromFile(filename);

                using (Graphics g = Graphics.FromImage(image))
                using (GraphicsPath path = new GraphicsPath())
                using (var stream = new MemoryStream())
                {
                    Rectangle rectangle = new Rectangle(x, y, h, w);
                    path.AddRectangle(rectangle);
                    g.DrawPath(Pens.Black, path);
                    image.Save(filename);
                }

                image.Save(filename);
            }

            public OcrCNIC(Boolean enableHovering = false)
            {
                _enableHovering = enableHovering;
            }
            #region Properties
            private Boolean _enableHovering = false;
            private List<dynamic> _position = new List<dynamic>();
            private List<dynamic> Positions
            {
                get
                {
                    if (_position == null) _position = new List<dynamic>();

                    if (_position.Count < 1)
                    {

                        var strPositions = System.IO.File.ReadAllLines(".positions");

                        foreach (var pos in strPositions)
                        {
                            var p = (pos.IndexOf("--") < 0) ? pos : pos.Substring(0, pos.IndexOf("--"));

                            if (string.IsNullOrEmpty(p)) continue;

                            var values = p.Split(",");
                            _position.Add(new
                            {
                                x = Int32.Parse(values[0].Trim()),
                                y = Int32.Parse(values[1].Trim()),
                                w = Int32.Parse(values[2].Trim()),
                                h = Int32.Parse(values[3].Trim()),
                                field = values[4].Trim(),
                                side = values[5].Trim(),
                                type = values[6].Trim(),
                            });
                        }
                    }
                    return _position;
                }
                set
                {
                    _position = value;
                }
            }
            #endregion

            #region EXPORTs
            public List<dynamic> CnicFront(string[] FileNames)
            {
                return Start(Positions.Where(p => p.side == "F" && p.type == "CNIC").ToList(), FileNames);
            }
            public List<dynamic> CnicBack(string[] FileNames)
            {
                return Start(Positions.Where(p => p.side == "B" && p.type == "CNIC").ToList(), FileNames);
            }
            public List<dynamic> NicopFront(string[] FileNames)
            {
                return Start(Positions.Where(p => p.side == "F" && p.type == "NICOP").ToList(), FileNames);
            }
            public List<dynamic> NicopBack(string[] FileNames)
            {
                return Start(Positions.Where(p => p.side == "B" && p.type == "NICOP").ToList(), FileNames);
            }
            public List<dynamic> OldFront(string[] FileNames)
            {
                return Start(Positions.Where(p => p.side == "F" && p.type == "OLDNIC").ToList(), FileNames);
            }
            public List<dynamic> OldBack(string[] FileNames)
            {
                return Start(Positions.Where(p => p.side == "B" && p.type == "OLDNIC").ToList(), FileNames);
            }
            #endregion

            #region START
            private List<dynamic> Start(List<dynamic> positions, string[] fileNames)
            {
                var result = new List<dynamic>();

                using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
                {
                    foreach (var fileName in fileNames)
                    {
                        using (var pixFromFile = Pix.LoadFromFile(fileName))
                        using (var pixResized = pixFromFile.Scale(2288f / pixFromFile.Width, 1434f / pixFromFile.Height))
                        //using (var pixeOtsu = pixResized.BinarizeOtsuAdaptiveThreshold(50, 50, 5, 5, 0.05f))
                        {
                            engine.SetVariable("tessedit_char_whitelist", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 /.-,\\");

                            pixResized.Save(@"C:\temp\" + fileName);
                            foreach (var pos in positions)
                            {
                                if (_enableHovering)
                                {
                                    dynamic previousConfidence = 0;
                                    dynamic r = null;
                                    for (int i = 0; i < 50; i += 10)
                                    {
                                        using (var page = engine.Process(pixResized, new Rect(pos.x, pos.y + i, pos.w, pos.h), PageSegMode.SingleLine))
                                        {
                                            Console.WriteLine("* {0,5} | x:{1,5} y:{2,5} w:{3,5} h:{4,5} Mean confidence: {5,5} | Text: {6,50} | File: {7}", pos.field, pos.x, pos.y + i, pos.w, pos.h, page.GetMeanConfidence(), page.GetText().Trim(), fileName);
                                            page.GetMeanConfidence();
                                            if (previousConfidence < page.GetMeanConfidence())
                                            {
                                                string text = page.GetText().Trim();

                                                // ignore list
                                                if (text.Contains("name", StringComparison.CurrentCultureIgnoreCase)
                                                    || text.Contains("gender", StringComparison.CurrentCultureIgnoreCase)
                                                    || text.Contains("country", StringComparison.CurrentCultureIgnoreCase)
                                                    || text.Contains("identity", StringComparison.CurrentCultureIgnoreCase)
                                                    || text.Contains("date", StringComparison.CurrentCultureIgnoreCase))
                                                    continue;

                                                // unwanted
                                                text = text.Replace("|", "").Replace("_", "").Replace("\"", "").Replace("~", "");

                                                //draw ract
                                                DrawRact(fileName, pos.x, pos.y + i, pos.w, pos.h);

                                                r = new
                                                {
                                                    x = pos.x,
                                                    y = pos.y + i,
                                                    w = pos.w,
                                                    h = pos.h,
                                                    field = pos.field,
                                                    side = pos.side,
                                                    type = pos.type,
                                                    text = text.Trim(),
                                                    confidence = page.GetMeanConfidence(),
                                                    fileName = fileName
                                                };
                                                previousConfidence = page.GetMeanConfidence();
                                            }
                                        }
                                    }

                                    if (r != null) result.Add(r);
                                }
                                else
                                {
                                    using (var page = engine.Process(pixResized, new Rect(pos.x, pos.y, pos.w, pos.h), PageSegMode.SingleLine))
                                    {
                                        Console.WriteLine("* {0,5} | x:{1,5} y:{2,5} w:{3,5} h:{4,5} Mean confidence: {5,5} | Text: {6,50} | File: {7}", pos.field, pos.x, pos.y, pos.w, pos.h, page.GetMeanConfidence(), page.GetText().Trim(), fileName);
                                        result.Add(new
                                        {
                                            x = pos.x,
                                            y = pos.y,
                                            w = pos.w,
                                            h = pos.h,
                                            field = pos.field,
                                            side = pos.side,
                                            type = pos.type,
                                            text = page.GetText().Trim(),
                                            confidence = page.GetMeanConfidence(),
                                            fileName = fileName
                                        });
                                    }

                                }
                            }

                        }
                    }
                }
                return result;
            }
            #endregion
        }
    }
}



//// ijra = new Rect(130, 888, 420, 100)
//// tanseekh = new Rect(550, 900, 420, 100)

//using Tesseract;
//using static System.Net.Mime.MediaTypeNames;

//namespace mytesseract
//{
//    internal class Program
//    {
//        static void Main(string[] args)
//        {
//            //OcrCNIC ocr = new OcrCNIC();
//            //ocr.Start(ocr.Positions);

// /*           //--
//            var Positions  = new List<dynamic>();
//            var strPositions = System.IO.File.ReadAllLines(".positions");

//            foreach (var pos in strPositions)
//            {
//                var p = (pos.IndexOf("--") < 0) ? pos : pos.Substring(0, pos.IndexOf("--"));

//                if (string.IsNullOrEmpty(p)) continue;

//                var values = p.Split(",");
//                Positions.Add(new
//                {
//                    x = Int32.Parse(values[0].Trim()),
//                    y = Int32.Parse(values[1].Trim()),
//                    w = Int32.Parse(values[2].Trim()),
//                    h = Int32.Parse(values[3].Trim()),
//                    field = values[4].Trim(),
//                    side = values[5].Trim(),
//                    type = values[6].Trim()
//                });
//            }

//            var pixList = new List<Pix>();
//            pixList.Add(Pix.LoadFromFile("sample1.back.cnic.jpg"));
//            pixList.Add(Pix.LoadFromFile("sample2.back.cnic.jpg"));

//            using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
//            {
                  //engine.SetVariable("tessedit_char_whitelist", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789/.-");
//                //engine.SetVariable("user_defined_dpi", "300");
//                foreach (var pix in pixList)
//                {
//                    Console.WriteLine("--------------------------");
//                    foreach (var pos in Positions)
//                    {
//                        using (var page = engine.Process(pix, new Rect(pos.x, pos.y, pos.w, pos.h)))
//                        {
//                            Console.WriteLine("* {0} | x:{1} y:{2} w:{3} h:{4} Mean confidence: {5} | Text: {6}", pos.field, pos.x, pos.y, pos.w, pos.h, page.GetMeanConfidence(), page.GetText().Trim());
//                        }
//                        using (var page = engine.Process(pix, new Rect(pos.x+10, pos.y+10, pos.w, pos.h)))
//                        {
//                            Console.WriteLine("* {0} | x:{1} y:{2} w:{3} h:{4} Mean confidence: {5} | Text: {6}", pos.field, pos.x + 10, pos.y + 10, pos.w, pos.h, page.GetMeanConfidence(), page.GetText().Trim());
//                        }
//                        using (var page = engine.Process(pix, new Rect(pos.x - 10, pos.y - 10, pos.w, pos.h)))
//                        {
//                            Console.WriteLine("* {0} | x:{1} y:{2} w:{3} h:{4} Mean confidence: {5} | Text: {6}", pos.field, pos.x - 10, pos.y - 10, pos.w, pos.h, page.GetMeanConfidence(), page.GetText().Trim());
//                        }
//                    }
//                }
//            }
//*/
//        }
//    }
//}