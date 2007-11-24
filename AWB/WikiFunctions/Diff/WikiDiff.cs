using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using Algorithm.Diff;
using System.Text.RegularExpressions;
using System.Collections;

namespace WikiFunctions
{
    /// <summary>
    /// This class renders MediaWiki-like HTML diffs
    /// </summary>
    public class WikiDiff
    {
        string[] LeftLines;
        string[] RightLines;
        Diff diff;
        StringBuilder Result;
        int ContextLines;

        /// <summary>
        /// Renders diff
        /// </summary>
        /// <param name="leftText">Earlier version of the text</param>
        /// <param name="rightText">Later version of the text</param>
        /// <param name="contextLines">Number of unchanged lines to show around changed ones</param>
        /// <returns>HTML diff</returns>
        public string GetDiff(string leftText, string rightText, int contextLines)
        {
            Result = new StringBuilder(500000);
            LeftLines = leftText.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            RightLines = rightText.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            ContextLines = contextLines;

            diff = new Diff(LeftLines, RightLines, false, true);
            foreach (Diff.Hunk h in diff)
            {
                if (h.Same) RenderContext(h);
                else RenderDifference(h);

            }
            return Result.ToString();
        }

        #region High-level diff stuff
        void RenderDifference(Diff.Hunk hunk)
        {
            Range left = hunk.Left;
            Range right = hunk.Right;

            if (right.Start == 0) ContextHeader(0, 0);
            int changes = Math.Min(left.Count, right.Count);
            for (int i = 0; i < changes; i++)
            {
                LineChanged(left.Start + i, right.Start + i);
            }
            if (left.Count > right.Count)
                for (int i = changes; i < left.Count; i++)
                    LineDeleted(left.Start + i, right.Start + changes);
            else if (left.Count < right.Count)
                for (int i = changes; i < right.Count; i++)
                    LineAdded(right.Start + i);
        }

        void RenderContext(Diff.Hunk hunk)
        {
            Range left = hunk.Left;
            Range right = hunk.Right;
            int displayed = 0;

            if (Result.Length > 0) // not the first hunk, adding context for previous change
            {
                displayed = Math.Min(ContextLines, right.Count);
                for (int i = 0; i < displayed; i++) ContextLine(right.Start + i);
            }

            int toDisplay = Math.Min(right.Count - displayed, ContextLines);
            if (right.End < RightLines.Length - 1 && toDisplay > 0)
            // not the last hunk, adding context for next change
            {
                if (right.Count > displayed + toDisplay) ContextHeader(left.End - toDisplay + 1, right.End - toDisplay + 1);
                for (int i = 0; i < toDisplay; i++) ContextLine(right.End - toDisplay + i + 1);
            }
        }

        void LineChanged(int leftLine, int rightLine)
        {
            // some kind of glitch with the diff engine
            if (LeftLines[leftLine] == RightLines[rightLine])
            {
                ContextLine(rightLine);
                return;
            }

            StringBuilder left = new StringBuilder();
            StringBuilder right = new StringBuilder();

            List<Word> leftList = Word.SplitString(LeftLines[leftLine]);
            List<Word> rightList = Word.SplitString(RightLines[rightLine]);

            Diff diff = new Diff(leftList, rightList, Word.Comparer, Word.Comparer);

            foreach (Diff.Hunk h in diff)
            {
                if (h.Same)
                {
                    for (int i = 0; i < h.Left.Count; i++)
                    {
                        WordDiff(left, rightList[h.Right.Start + i], leftList[h.Left.Start + i]);
                        WordDiff(right, leftList[h.Left.Start + i], rightList[h.Right.Start + i]);
                    }
                }
                else
                {
                    left.Append("<span class='diffchange'>");
                    for (int i = h.Left.Start; i <= h.Left.End; i++)
                        left.Append(HttpUtility.HtmlEncode(leftList[i].ToString()));
                    left.Append("</span>");

                    right.Append("<span class='diffchange'>");
                    for (int i = h.Right.Start; i <= h.Right.End; i++)
                        right.Append(HttpUtility.HtmlEncode(rightList[i].ToString()));
                    right.Append("</span>");
                }
            }

            Result.AppendFormat(@"<tr onclick='window.external.GoTo({1})' ondblclick='window.external.UndoChange({0},{1})'>
  <td>-</td>
  <td class='diff-deletedline'>", leftLine, rightLine);
            Result.Append(left);
            Result.Append(@"  </td>
  <td>+</td>
  <td class='diff-addedline'>");
            Result.Append(right);
            Result.Append(@"  </td>
		</tr>");
        }

        void WordDiff(StringBuilder res, Word left, Word right)
        {
            if (left.Whitespace == right.Whitespace) res.Append(HttpUtility.HtmlEncode(right.ToString()));
            else
            {
                res.Append(HttpUtility.HtmlEncode(right.TheWord));
                char[] leftChars = left.Whitespace.ToCharArray();
                char[] rightChars = right.Whitespace.ToCharArray();

                Diff diff = new Diff(leftChars, rightChars, Word.Comparer, Word.Comparer);
                foreach (Diff.Hunk h in diff)
                {
                    if (h.Same)
                        res.Append(rightChars, h.Right.Start, h.Right.Count);
                    else
                    {
                        res.Append("<span class='diffchange'>");
                        res.Append('\x00A0', h.Right.Count); // replace spaces with NBSPs to make 'em visible
                        res.Append("</span>");
                    }
                }
            }
        }
        #endregion

        #region Visualisation primitives
        /// <summary>
        /// Renders a context row
        /// </summary>
        /// <param name="line">Number of line in the RIGHT text</param>
        void ContextLine(int line)
        {
            string html = HttpUtility.HtmlEncode(RightLines[line]);
            Result.AppendFormat(@"<tr onclick='window.external.GoTo({0});'>
  <td> </td>
  <td class='diff-context'>", line);
            Result.Append(html);
            Result.Append(@"</td>
  <td> </td>
  <td class='diff-context'>");
            Result.Append(html);
            Result.Append(@"</td>
</tr>");
        }

        void LineDeleted(int left, int right)
        {
            Result.AppendFormat(@"<tr>
  <td>-</td>
  <td class='diff-deletedline' onclick='window.external.GoTo({1})' ondblclick='window.external.UndoDeletion({0}, {1})'>",
                left, right);
            Result.Append(HttpUtility.HtmlEncode(LeftLines[left]));
            Result.Append(@"  </td>
  <td> </td>
  <td> </td>
</tr>");
        }

        void LineAdded(int line)
        {
            Result.AppendFormat(@"<tr>
  <td> </td>
  <td> </td>
  <td>+</td>
  <td class='diff-addedline' onclick='window.external.GoTo({0})' ondblclick='window.external.UndoAddition({0})'>", line);
            Result.Append(HttpUtility.HtmlEncode(RightLines[line]));
            Result.Append(@"  </td>
</tr>");
        }

        void ContextHeader(int left, int right)
        {
            Result.AppendFormat(@"<tr onclick='window.external.GoTo({2})'>
  <td colspan='2' align='left'><strong>Line {0}</strong></td>
  <td colspan='2' align='left'><strong>Line {1}</strong></td>
</tr>", left + 1, right + 1, right);
        }
        #endregion

        #region Undo
        public string UndoChange(int left, int right)
        {
            RightLines[right] = LeftLines[left];

            return string.Join("\r\n", RightLines);
        }

        public string UndoAddition(int right)
        {
            StringBuilder s = new StringBuilder();

            for(int i = 0;i<RightLines.Length; i++)
                if (i != right)
                {
                    if (i>0) s.Append("\r\n");
                    s.Append(RightLines[i]);
                }

            //s.Remove("

            return s.ToString();
        }

        public string UndoDeletion(int left, int right)
        {
            StringBuilder s = new StringBuilder();

            for (int i = 0; i < RightLines.Length; i++)
            {
                if (i == right)
                {
                    s.Append(LeftLines[left]);
                    s.Append("\r\n");
                }
                s.Append(RightLines[i]);
                s.Append("\r\n");
            }

            return s.ToString();
        }

        #endregion

        #region Static methods
        public static string TableHeader
        {
            get
            {
                return @"<p style='font-family: arial; size:75%;'>Double-click on a line to undo all changes on that line, or single click to focus the edit box to that line.</p>
<table border='0' width='98%' cellpadding='0' cellspacing='4' class='diff'>
	<tr>
		<td colspan='2' width='50%' align='center' class='diff-otitle'><strong>Current revision</strong></td>
		<td colspan='2' width='50%' align='center' class='diff-ntitle'><strong>Your text</strong></td>
	</tr>
	<tr height='0px'>
		<td width='1'></td>
		<td width='50%'></td>
		<td width='1'></td>
		<td width='50%'></td>
	</tr>
";
            }
        }

        public static string DefaultStyles
        {
            get
            {
                return @"
td{
    border: 1px solid white;
}

table.diff, td.diff-otitle, td.diff-ntitle {
	background-color: white;
    border: 1px solid gray;
}
td.diff-addedline {
	background: #cfc;
	font-size: smaller;
}
td.diff-deletedline {
	background: #ffa;
	font-size: smaller;
}
td.diff-context {
	background: #eee;
	font-size: smaller;
}
.diffchange {
	color: red;
	font-weight: bold;
	text-decoration: none;
}

td.diff-deletedline span.diffchange {
    background-color: #FFD754; color:black;
}

td.diff-addedline span.diffchange {
    background-color: #73E5A1; color:black;
}

.d{
    overflow: auto;
}
";
            }
        }

        static string CustomStyles = null;

        public static string DiffHead()
        {
            string styles = DefaultStyles;

            if (!string.IsNullOrEmpty(CustomStyles))
                styles = CustomStyles;
            else if (System.IO.File.Exists("style.css") && CustomStyles == null)
            {
                try
                {
                    System.IO.StreamReader reader = System.IO.File.OpenText("style.css");
                    CustomStyles = reader.ReadToEnd();
                    styles = CustomStyles;
                }
                catch
                {
                    CustomStyles = "";
                }
            }

            return "<style type='text/css'>" + styles + "</style>";
        }

        public static void ResetCustomStyles()
        {
            CustomStyles = null;
        }
        #endregion
    }

    internal class Word
    {
        public string TheWord;
        public string Whitespace;

        int HashCode;

        public Word(string word, string white)
        {
            TheWord = word;
            Whitespace = white;
            HashCode = (TheWord + Whitespace).GetHashCode();
        }

        /// <summary>
        /// Too slow, don't use
        /// </summary>
        /// <param name="all">Word with whitespace</param>
        public Word(string all)
            :this(Regex.Match(all, @"\S*").Value, Regex.Match(all, @"\s*").Value)
        {
        }

        public static WordComparer Comparer = new WordComparer();

        static readonly Regex Splitter = new Regex(@"(\p{P}|[^\s\p{P}]*)(\s*)", RegexOptions.Compiled);

        public static List<Word> SplitString(string s)
        {
            List<Word> lst = new List<Word>();

            foreach (Match m in Splitter.Matches(s))
                if (m.Value.Length > 0) lst.Add(new Word(m.Groups[1].Value, m.Groups[2].Value));

            return lst;
        }

        #region Overrides
        public override string ToString()
        {
            return TheWord + Whitespace;
        }

        public override bool Equals(object obj)
        {
            return TheWord.Equals((obj as Word).TheWord);
        }

        public override int GetHashCode()
        {
            return HashCode;
        }
        #endregion
    }

    internal class WordComparer : IComparer, IHashCodeProvider
    {
        public int GetHashCode(object obj)
        {
            return obj.GetHashCode();
        }

        public int Compare(object x, object y)
        {
            return x.Equals(y) ? 0 : 1;
        }
    }
}
