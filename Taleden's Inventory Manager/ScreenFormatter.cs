using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;

namespace IngameScript
{
    class ScreenFormatter
    {
        private static Dictionary<char, byte> charWidth = new Dictionary<char, byte>();
        private static Dictionary<string, int> textWidth = new Dictionary<string, int>();
        private static byte SZ_SPACE;
        private static byte SZ_SHYPH;

        public static int GetWidth(string text, bool memoize = false)
        {
            int width;
            if (!textWidth.TryGetValue(text, out width))
            {
                // this isn't faster (probably slower) but it's less "complex"
                // according to SE's silly branch count metric
                Dictionary<char, byte> cW = charWidth;
                string t = text + "\0\0\0\0\0\0\0";
                int i = t.Length - t.Length % 8;
                byte w0, w1, w2, w3, w4, w5, w6, w7;
                while (i > 0)
                {
                    cW.TryGetValue(t[i - 1], out w0);
                    cW.TryGetValue(t[i - 2], out w1);
                    cW.TryGetValue(t[i - 3], out w2);
                    cW.TryGetValue(t[i - 4], out w3);
                    cW.TryGetValue(t[i - 5], out w4);
                    cW.TryGetValue(t[i - 6], out w5);
                    cW.TryGetValue(t[i - 7], out w6);
                    cW.TryGetValue(t[i - 8], out w7);
                    width += w0 + w1 + w2 + w3 + w4 + w5 + w6 + w7;
                    i -= 8;
                }
                if (memoize)
                    textWidth[text] = width;
            }
            return width;
        }

        public static string Format(string text, int width, out int unused, int align = -1, bool memoize = false)
        {
            int spaces, bars;

            // '\u00AD' is a "soft hyphen" in UTF16 but Panels don't wrap lines so
            // it's just a wider space character ' ', useful for column alignment
            unused = width - GetWidth(text, memoize);
            if (unused <= SZ_SPACE / 2)
                return text;
            spaces = unused / SZ_SPACE;
            bars = 0;
            unused -= spaces * SZ_SPACE;
            if (2 * unused <= SZ_SPACE + spaces * (SZ_SHYPH - SZ_SPACE))
            {
                bars = Math.Min(spaces, (int)((float)unused / (SZ_SHYPH - SZ_SPACE) + 0.4999f));
                spaces -= bars;
                unused -= bars * (SZ_SHYPH - SZ_SPACE);
            }
            else if (unused > SZ_SPACE / 2)
            {
                spaces++;
                unused -= SZ_SPACE;
            }
            if (align > 0)
                return new String(' ', spaces) + new String('\u00AD', bars) + text;
            if (align < 0)
                return text + new String('\u00AD', bars) + new String(' ', spaces);
            if (spaces % 2 > 0 & bars % 2 == 0)
                return new String(' ', spaces / 2) + new String('\u00AD', bars / 2) + text + new String('\u00AD', bars / 2) + new String(' ', spaces - spaces / 2);
            return new String(' ', spaces - spaces / 2) + new String('\u00AD', bars / 2) + text + new String('\u00AD', bars - bars / 2) + new String(' ', spaces / 2);
        }

        public static string Format(double value, int width, out int unused)
        {
            int spaces, bars;
            value = Math.Min(Math.Max(value, 0.0f), 1.0f);
            spaces = width / SZ_SPACE;
            bars = (int)(spaces * value + 0.5f);
            unused = width - spaces * SZ_SPACE;
            return new String('I', bars) + new String(' ', spaces - bars);
        }

        public static void Init()
        {
            InitChars(0, "\u2028\u2029\u202F");
            InitChars(7, "'|\u00A6\u02C9\u2018\u2019\u201A");
            InitChars(8, "\u0458");
            InitChars(9, " !I`ijl\u00A0\u00A1\u00A8\u00AF\u00B4\u00B8\u00CC\u00CD\u00CE\u00CF\u00EC\u00ED\u00EE\u00EF\u0128\u0129\u012A\u012B\u012E\u012F\u0130\u0131\u0135\u013A\u013C\u013E\u0142\u02C6\u02C7\u02D8\u02D9\u02DA\u02DB\u02DC\u02DD\u0406\u0407\u0456\u0457\u2039\u203A\u2219");
            InitChars(10, "(),.1:;[]ft{}\u00B7\u0163\u0165\u0167\u021B");
            InitChars(11, "\"-r\u00AA\u00AD\u00BA\u0140\u0155\u0157\u0159");
            InitChars(12, "*\u00B2\u00B3\u00B9");
            InitChars(13, "\\\u00B0\u201C\u201D\u201E");
            InitChars(14, "\u0491");
            InitChars(15, "/\u0133\u0442\u044D\u0454");
            InitChars(16, "L_vx\u00AB\u00BB\u0139\u013B\u013D\u013F\u0141\u0413\u0433\u0437\u043B\u0445\u0447\u0490\u2013\u2022");
            InitChars(17, "7?Jcz\u00A2\u00BF\u00E7\u0107\u0109\u010B\u010D\u0134\u017A\u017C\u017E\u0403\u0408\u0427\u0430\u0432\u0438\u0439\u043D\u043E\u043F\u0441\u044A\u044C\u0453\u0455\u045C");
            InitChars(18, "3FKTabdeghknopqsuy\u00A3\u00B5\u00DD\u00E0\u00E1\u00E2\u00E3\u00E4\u00E5\u00E8\u00E9\u00EA\u00EB\u00F0\u00F1\u00F2\u00F3\u00F4\u00F5\u00F6\u00F8\u00F9\u00FA\u00FB\u00FC\u00FD\u00FE\u00FF\u00FF\u0101\u0103\u0105\u010F\u0111\u0113\u0115\u0117\u0119\u011B\u011D\u011F\u0121\u0123\u0125\u0127\u0136\u0137\u0144\u0146\u0148\u0149\u014D\u014F\u0151\u015B\u015D\u015F\u0161\u0162\u0164\u0166\u0169\u016B\u016D\u016F\u0171\u0173\u0176\u0177\u0178\u0219\u021A\u040E\u0417\u041A\u041B\u0431\u0434\u0435\u043A\u0440\u0443\u0446\u044F\u0451\u0452\u045B\u045E\u045F");
            InitChars(19, "+<=>E^~\u00AC\u00B1\u00B6\u00C8\u00C9\u00CA\u00CB\u00D7\u00F7\u0112\u0114\u0116\u0118\u011A\u0404\u040F\u0415\u041D\u042D\u2212");
            InitChars(20, "#0245689CXZ\u00A4\u00A5\u00C7\u00DF\u0106\u0108\u010A\u010C\u0179\u017B\u017D\u0192\u0401\u040C\u0410\u0411\u0412\u0414\u0418\u0419\u041F\u0420\u0421\u0422\u0423\u0425\u042C\u20AC");
            InitChars(21, "$&GHPUVY\u00A7\u00D9\u00DA\u00DB\u00DC\u00DE\u0100\u011C\u011E\u0120\u0122\u0124\u0126\u0168\u016A\u016C\u016E\u0170\u0172\u041E\u0424\u0426\u042A\u042F\u0436\u044B\u2020\u2021");
            InitChars(22, "ABDNOQRS\u00C0\u00C1\u00C2\u00C3\u00C4\u00C5\u00D0\u00D1\u00D2\u00D3\u00D4\u00D5\u00D6\u00D8\u0102\u0104\u010E\u0110\u0143\u0145\u0147\u014C\u014E\u0150\u0154\u0156\u0158\u015A\u015C\u015E\u0160\u0218\u0405\u040A\u0416\u0444");
            InitChars(23, "\u0459");
            InitChars(24, "\u044E");
            InitChars(25, "%\u0132\u042B");
            InitChars(26, "@\u00A9\u00AE\u043C\u0448\u045A");
            InitChars(27, "M\u041C\u0428");
            InitChars(28, "mw\u00BC\u0175\u042E\u0449");
            InitChars(29, "\u00BE\u00E6\u0153\u0409");
            InitChars(30, "\u00BD\u0429");
            InitChars(31, "\u2122");
            InitChars(32, "W\u00C6\u0152\u0174\u2014\u2026\u2030");
            SZ_SPACE = charWidth[' '];
            SZ_SHYPH = charWidth['\u00AD'];
        }

        private static void InitChars(byte width, string text)
        {
            // more silly loop-unrolling, as in GetWidth()
            Dictionary<char, byte> cW = charWidth;
            string t = text + "\0\0\0\0\0\0\0";
            byte w = Math.Max((byte)0, width);
            int i = t.Length - t.Length % 8;
            while (i > 0)
            {
                cW[t[--i]] = w;
                cW[t[--i]] = w;
                cW[t[--i]] = w;
                cW[t[--i]] = w;
                cW[t[--i]] = w;
                cW[t[--i]] = w;
                cW[t[--i]] = w;
                cW[t[--i]] = w;
            }
            cW['\0'] = 0;
        } // InitChars()

        private int numCols;
        private int numRows;
        private int padding;
        private List<string>[] colRowText;
        private List<int>[] colRowWidth;
        private int[] colAlign;
        private int[] colFill;
        private bool[] colBar;
        private int[] colWidth;

        public ScreenFormatter(int numCols, int padding = 1)
        {
            this.numCols = numCols;
            numRows = 0;
            this.padding = padding;
            colRowText = new List<string>[numCols];
            colRowWidth = new List<int>[numCols];
            colAlign = new int[numCols];
            colFill = new int[numCols];
            colBar = new bool[numCols];
            colWidth = new int[numCols];
            for (int c = 0; c < numCols; c++)
            {
                colRowText[c] = new List<string>();
                colRowWidth[c] = new List<int>();
                colAlign[c] = -1;
                colFill[c] = 0;
                colBar[c] = false;
                colWidth[c] = 0;
            }
        }

        public void Add(int col, string text, bool memoize = false)
        {
            int width = 0;
            colRowText[col].Add(text);
            if (colBar[col] == false)
            {
                width = GetWidth(text, memoize);
                colWidth[col] = Math.Max(colWidth[col], width);
            }
            colRowWidth[col].Add(width);
            numRows = Math.Max(numRows, colRowText[col].Count);
        }

        public void AddBlankRow()
        {
            for (int c = 0; c < numCols; c++)
            {
                colRowText[c].Add("");
                colRowWidth[c].Add(0);
            }
            numRows++;
        }

        public int GetNumRows()
        {
            return numRows;
        }

        public int GetMinWidth()
        {
            int width = padding * SZ_SPACE;
            for (int c = 0; c < numCols; c++)
                width += padding * SZ_SPACE + colWidth[c];
            return width;
        }

        public void SetAlign(int col, int align)
        {
            colAlign[col] = align;
        }

        public void SetFill(int col, int fill = 1)
        {
            colFill[col] = fill;
        }

        public void SetBar(int col, bool bar = true)
        {
            colBar[col] = bar;
        }

        public void SetWidth(int col, int width)
        {
            colWidth[col] = width;
        }

        public string[][] ToSpan(int width = 0, int span = 1)
        {
            int c, r, s, i, j, textwidth, unused, remaining;
            int[] colWidth;
            byte w;
            double value;
            string text;
            StringBuilder sb;
            string[][] spanLines;

            // clone the user-defined widths and tally fill columns
            colWidth = (int[])this.colWidth.Clone();
            unused = width * span - padding * SZ_SPACE;
            remaining = 0;
            for (c = 0; c < numCols; c++)
            {
                unused -= padding * SZ_SPACE;
                if (colFill[c] == 0)
                    unused -= colWidth[c];
                remaining += colFill[c];
            }

            // distribute remaining width to fill columns
            for (c = 0; c < numCols & remaining > 0; c++)
            {
                if (colFill[c] > 0)
                {
                    colWidth[c] = Math.Max(colWidth[c], colFill[c] * unused / remaining);
                    unused -= colWidth[c];
                    remaining -= colFill[c];
                }
            }

            // initialize output arrays
            spanLines = new string[span][];
            for (s = 0; s < span; s++)
                spanLines[s] = new string[numRows];
            span--; // make "span" inclusive so "s < span" implies one left

            // render all rows and columns
            i = 0;
            sb = new StringBuilder();
            for (r = 0; r < numRows; r++)
            {
                sb.Clear();
                s = 0;
                remaining = width;
                unused = 0;
                for (c = 0; c < numCols; c++)
                {
                    unused += padding * SZ_SPACE;
                    if (r >= colRowText[c].Count || colRowText[c][r] == "")
                    {
                        unused += colWidth[c];
                    }
                    else
                    {
                        // render the bar, or fetch the cell text
                        text = colRowText[c][r];
                        charWidth.TryGetValue(text[0], out w);
                        textwidth = colRowWidth[c][r];
                        if (colBar[c])
                        {
                            if (double.TryParse(text, out value))
                                value = Math.Min(Math.Max(value, 0.0), 1.0);
                            i = (int)(colWidth[c] / SZ_SPACE * value + 0.5);
                            w = SZ_SPACE;
                            textwidth = i * SZ_SPACE;
                        }

                        // if the column is not left-aligned, calculate left spacing
                        if (colAlign[c] > 0)
                        {
                            unused += colWidth[c] - textwidth;
                        }
                        else if (colAlign[c] == 0)
                        {
                            unused += (colWidth[c] - textwidth) / 2;
                        }

                        // while the left spacing leaves no room for text, adjust it
                        while (s < span & unused > remaining - w)
                        {
                            sb.Append(' ');
                            spanLines[s][r] = sb.ToString();
                            sb.Clear();
                            s++;
                            unused -= remaining;
                            remaining = width;
                        }

                        // add left spacing
                        remaining -= unused;
                        sb.Append(Format("", unused, out unused));
                        remaining += unused;

                        // if the column is not right-aligned, calculate right spacing
                        if (colAlign[c] < 0)
                        {
                            unused += colWidth[c] - textwidth;
                        }
                        else if (colAlign[c] == 0)
                        {
                            unused += colWidth[c] - textwidth - (colWidth[c] - textwidth) / 2;
                        }

                        // while the bar or text runs to the next span, split it
                        if (colBar[c])
                        {
                            while (s < span & textwidth > remaining)
                            {
                                j = remaining / SZ_SPACE;
                                remaining -= j * SZ_SPACE;
                                textwidth -= j * SZ_SPACE;
                                sb.Append(new String('I', j));
                                spanLines[s][r] = sb.ToString();
                                sb.Clear();
                                s++;
                                unused -= remaining;
                                remaining = width;
                                i -= j;
                            }
                            text = new String('I', i);
                        }
                        else
                        {
                            while (s < span & textwidth > remaining)
                            {
                                i = 0;
                                while (remaining >= w)
                                {
                                    remaining -= w;
                                    textwidth -= w;
                                    charWidth.TryGetValue(text[++i], out w);
                                }
                                sb.Append(text, 0, i);
                                spanLines[s][r] = sb.ToString();
                                sb.Clear();
                                s++;
                                unused -= remaining;
                                remaining = width;
                                text = text.Substring(i);
                            }
                        }

                        // add cell text
                        remaining -= textwidth;
                        sb.Append(text);
                    }
                }
                spanLines[s][r] = sb.ToString();
            }

            return spanLines;
        }

        public string ToString(int width = 0)
        {
            return String.Join("\n", ToSpan(width)[0]);
        }

    }
}
