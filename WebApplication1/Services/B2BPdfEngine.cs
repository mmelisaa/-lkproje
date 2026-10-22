using System.Text;

namespace WebApplication1.Services
{
    /// <summary>
    /// Harici üçüncü parti kütüphaneye ihtiyaç duymadan geçerli ve temiz PDF-1.4 belgeleri
    /// üreten yüksek performanslı hafif PDF motoru.
    /// </summary>
    public class B2BPdfEngine
    {
        private readonly double _pageWidth;
        private readonly double _pageHeight;
        private readonly StringBuilder _content = new();

        public B2BPdfEngine(double width = 595.28, double height = 841.89) // A4 Boyutları (pt)
        {
            _pageWidth = width;
            _pageHeight = height;
        }

        public double PageWidth => _pageWidth;
        public double PageHeight => _pageHeight;

        public void DrawRect(double x, double y, double width, double height, double r, double g, double b, bool fill = true, bool stroke = false, double strokeR = 0, double strokeG = 0, double strokeB = 0, double lineWidth = 1)
        {
            _content.AppendLine("q");
            if (stroke)
            {
                _content.AppendLine($"{lineWidth:F2} w");
                _content.AppendLine($"{strokeR:F3} {strokeG:F3} {strokeB:F3} RG");
            }
            if (fill)
            {
                _content.AppendLine($"{r:F3} {g:F3} {b:F3} rg");
            }
            _content.AppendLine($"{x:F2} {y:F2} {width:F2} {height:F2} re");
            if (fill && stroke) _content.AppendLine("B");
            else if (fill) _content.AppendLine("f");
            else if (stroke) _content.AppendLine("S");
            _content.AppendLine("Q");
        }

        public void DrawLine(double x1, double y1, double x2, double y2, double r = 0.8, double g = 0.8, double b = 0.8, double width = 1)
        {
            _content.AppendLine("q");
            _content.AppendLine($"{width:F2} w");
            _content.AppendLine($"{r:F3} {g:F3} {b:F3} RG");
            _content.AppendLine($"{x1:F2} {y1:F2} m");
            _content.AppendLine($"{x2:F2} {y2:F2} l");
            _content.AppendLine("S");
            _content.AppendLine("Q");
        }

        public void DrawText(string text, double x, double y, double fontSize = 10, bool isBold = false, double r = 0, double g = 0, double b = 0)
        {
            if (string.IsNullOrEmpty(text)) return;
            var safeText = SanitizeText(text);

            _content.AppendLine("BT");
            _content.AppendLine(isBold ? $"/F2 {fontSize:F1} Tf" : $"/F1 {fontSize:F1} Tf");
            _content.AppendLine($"{r:F3} {g:F3} {b:F3} rg");
            _content.AppendLine($"{x:F2} {y:F2} Td");
            _content.AppendLine($"({safeText}) Tj");
            _content.AppendLine("ET");
        }

        public void DrawWrappedText(string text, double x, ref double currentY, double maxWidth, double fontSize = 9, bool isBold = false, double r = 0.2, double g = 0.2, double b = 0.2, double lineHeight = 13)
        {
            if (string.IsNullOrEmpty(text)) return;
            var words = text.Split(' ');
            var sb = new StringBuilder();

            // Karakter başına yaklaşık genişlik (Helvetica orantısı)
            var approxCharWidth = fontSize * 0.52;
            var maxCharsPerLine = (int)(maxWidth / approxCharWidth);
            if (maxCharsPerLine < 10) maxCharsPerLine = 10;

            foreach (var word in words)
            {
                if (sb.Length + word.Length + 1 > maxCharsPerLine)
                {
                    DrawText(sb.ToString(), x, currentY, fontSize, isBold, r, g, b);
                    currentY -= lineHeight;
                    sb.Clear();
                }
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(word);
            }

            if (sb.Length > 0)
            {
                DrawText(sb.ToString(), x, currentY, fontSize, isBold, r, g, b);
                currentY -= lineHeight;
            }
        }

        public byte[] BuildPdfBytes()
        {
            var contentStr = _content.ToString();
            var contentBytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(contentStr);

            var objects = new List<byte[]>();
            var offsets = new List<long>();

            // Obj 1: Catalog
            objects.Add(Encoding.ASCII.GetBytes("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n"));
            // Obj 2: Pages
            objects.Add(Encoding.ASCII.GetBytes("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n"));
            // Obj 3: Page
            objects.Add(Encoding.ASCII.GetBytes($"3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {_pageWidth:F2} {_pageHeight:F2}] /Contents 4 0 R /Resources << /Font << /F1 5 0 R /F2 6 0 R >> >> >>\nendobj\n"));
            // Obj 4: Content Stream
            var streamHeader = Encoding.ASCII.GetBytes($"4 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n");
            var streamFooter = Encoding.ASCII.GetBytes("\nendstream\nendobj\n");
            var fullStream = new byte[streamHeader.Length + contentBytes.Length + streamFooter.Length];
            Buffer.BlockCopy(streamHeader, 0, fullStream, 0, streamHeader.Length);
            Buffer.BlockCopy(contentBytes, 0, fullStream, streamHeader.Length, contentBytes.Length);
            Buffer.BlockCopy(streamFooter, 0, fullStream, streamHeader.Length + contentBytes.Length, streamFooter.Length);
            objects.Add(fullStream);

            // Obj 5: Font F1 (Helvetica)
            objects.Add(Encoding.ASCII.GetBytes("5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>\nendobj\n"));
            // Obj 6: Font F2 (Helvetica-Bold)
            objects.Add(Encoding.ASCII.GetBytes("6 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>\nendobj\n"));

            using var ms = new MemoryStream();
            var header = Encoding.ASCII.GetBytes("%PDF-1.4\n%\xe2\xe3\xcf\xd3\n");
            ms.Write(header, 0, header.Length);

            long currentOffset = header.Length;
            for (int i = 0; i < objects.Count; i++)
            {
                offsets.Add(currentOffset);
                ms.Write(objects[i], 0, objects[i].Length);
                currentOffset += objects[i].Length;
            }

            // Xref tablosu
            long startXref = currentOffset;
            var xrefSb = new StringBuilder();
            xrefSb.AppendLine("xref");
            xrefSb.AppendLine($"0 {objects.Count + 1}");
            xrefSb.AppendLine("0000000000 65535 f ");
            foreach (var off in offsets)
            {
                xrefSb.AppendLine($"{off:D10} 00000 n ");
            }
            xrefSb.AppendLine("trailer");
            xrefSb.AppendLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
            xrefSb.AppendLine("startxref");
            xrefSb.AppendLine(startXref.ToString());
            xrefSb.AppendLine("%%EOF");

            var trailerBytes = Encoding.ASCII.GetBytes(xrefSb.ToString());
            ms.Write(trailerBytes, 0, trailerBytes.Length);

            return ms.ToArray();
        }

        private static string SanitizeText(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // Türkçe karakterleri standart PDF WinAnsi fontlarında hatasız ve pürüzsüz görünmesi için dönüştür
            var sb = new StringBuilder(input.Length);
            foreach (var ch in input)
            {
                switch (ch)
                {
                    case 'ı': sb.Append('i'); break;
                    case 'İ': sb.Append('I'); break;
                    case 'ş': sb.Append('s'); break;
                    case 'Ş': sb.Append('S'); break;
                    case 'ğ': sb.Append('g'); break;
                    case 'Ğ': sb.Append('G'); break;
                    case 'ü': sb.Append('u'); break;
                    case 'Ü': sb.Append('U'); break;
                    case 'ö': sb.Append('o'); break;
                    case 'Ö': sb.Append('O'); break;
                    case 'ç': sb.Append('c'); break;
                    case 'Ç': sb.Append('C'); break;
                    case '(': sb.Append("\\("); break;
                    case ')': sb.Append("\\)"); break;
                    case '\\': sb.Append("\\\\"); break;
                    default:
                        if (ch >= 32 && ch <= 126) sb.Append(ch);
                        else if (ch == '\n' || ch == '\r' || ch == '\t') sb.Append(' ');
                        else sb.Append(' ');
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
