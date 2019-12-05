using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;

using JetBrains.Annotations;

using Microsoft.Win32;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using RichTextBox = System.Windows.Controls.RichTextBox;

namespace Grail
{
    public static class Extensions
    {
        #region Constants
        // ReSharper disable MemberCanBePrivate.Global

        public const string FileStampFormat = "yyyyMMdd_HHmmss";

        public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

        public const string DateFormat = "yyyy-MM-dd";

        public const string TimeFormat = "HH:mm:ss";

        private const string RandomChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        public static readonly Random Rng = new Random();

        // ReSharper restore MemberCanBePrivate.Global
        #endregion Constants

        public static object[] Args(params object[] args)
        {
            return args;
        }

        public static string StripLeadingDirectorySeparator(this string @this)
        {
            return @this.TrimStart(Path.DirectorySeparatorChar)
                .TrimStart(Path.AltDirectorySeparatorChar);
        }

        public static void AddRange<T>(this ConcurrentBag<T> @this, IEnumerable<T> toAdd)
        {
            foreach (var element in toAdd)
            {
                @this.Add(element);
            }
        }

        public static string AskForFileToOpen(string initialDirectory = null, string ext = ".txt", string filter = "Text documents (.txt)|*.txt|All(*.*)|*.*")
        {
            var dlg = new OpenFileDialog
            {
                DefaultExt = ext,   // Default file extension
                Filter = filter,    // Filter files by extension 
                InitialDirectory = initialDirectory ?? AppDomain.CurrentDomain.BaseDirectory
            };

            // Process open file dialog box results 
            return dlg.ShowDialog().Equals(true)
                ? dlg.FileName
                : null;
        }

        public static string AskForFileToSave(string fileName = "", string initialDirectory = null, string ext = ".log", string filter = "Text documents (.log)|*.log|All(*.*)|*.*")
        {
            var dlg = new SaveFileDialog
            {
                FileName = fileName,
                DefaultExt = ext,   // Default file extension
                Filter = filter,    // Filter files by extension 
                InitialDirectory = initialDirectory ?? AppDomain.CurrentDomain.BaseDirectory
            };

            // Process open file dialog box results 
            return dlg.ShowDialog().Equals(true)
                ? dlg.FileName
                : null;
        }

        public static List<string> ToList(this StringCollection collection)
        {
            var result = new List<string>();
            foreach (var element in collection)
            {
                result.Add(element);
            }
            return result;
        }

        public static string[] SplitLines(this string multiLine)
        {
            return multiLine.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        }

        public static bool Matches(this string value, Regex regex)
        {
            return regex.IsMatch(value);
        }

        public static bool IsOneOf(this string value, StringCollection collection, StringComparison comparison = StringComparison.InvariantCultureIgnoreCase)
        {
            return collection.OfType<string>().Any(e => value.Equals(e, comparison));
        }


        public static string RemoveRoot(string path)
        {
            if (!Path.IsPathRooted(path)) return path;
            var root = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(root)) return path;

            if (root.ToUpperInvariant() != "M:\\") return path.Substring(root.Length);


            var directory = new DirectoryInfo(path);
            var viewRoot = "";
            while (directory.Parent != null && directory.Parent.Name != directory.Root.Name)
            {
                viewRoot = directory.Parent.Name;
                directory = directory.Parent;
            }

            var viewRootPath = Path.Combine(root, viewRoot);
            var newPath = path.Substring(viewRootPath.Length + 1);
            return newPath;
        }

        public static string RemoveFromEnd(this string value, string phrase)
        {
            return value?.Substring(0, value.Length - phrase.Length);
        }

        public static string RemoveFromEnd(this string value, IEnumerable<string> collection, StringComparison comparison = StringComparison.InvariantCultureIgnoreCase)
        {
            var array = collection.ToArray();
            while (value.EndsWith(array, comparison))
            {
                foreach (var phrase in array)
                {
                    if (value.EndsWith(phrase, comparison)) value = value.RemoveFromEnd(phrase);
                }
            }

            return value;
        }

        public static bool Contains(this string value, IEnumerable<string> collection, StringComparison comparison = StringComparison.InvariantCultureIgnoreCase)
        {
            return collection.Any(s => value.Contains(s, comparison));
        }

        public static bool StartsWith(this string value, IEnumerable<string> collection, StringComparison comparison = StringComparison.InvariantCultureIgnoreCase)
        {
            return collection.Any(s => value.StartsWith(s, comparison));
        }

        public static bool EndsWith(this string value, IEnumerable<string> collection, StringComparison comparison = StringComparison.InvariantCultureIgnoreCase)
        {
            return collection.Any(s => value.EndsWith(s, comparison));
        }



        public static string CurrentMethod([CallerMemberName] string callingMethod = "", [CallerFilePath] string callingPath = "", object[] args = null, object arg = null, object type = null)
        {
            var @class = Path.GetFileNameWithoutExtension(callingPath);

            if (type != null) @class += $"<{type}>";

            var arguments = new List<string>();

            if (arg != null && arg.ToString().HasValue())
            {
                arguments.Add(arg.ToString());
            }

            if (args != null)
            {
                foreach (var o in args)
                {
                    if (o == null) continue;
                    string value = null;
                    switch (o)
                    {
                        case DependencyObject _:
                            break;

                        case DependencyPropertyChangedEventArgs depProp:

                            value = $"{depProp.Property} : `{depProp.OldValue ?? "null"}` => `{depProp.NewValue ?? "null"}`";
                            break;
                    }

                    arguments.Add(value ?? o.ToString());
                }
            }

            var output = callingMethod == ".ctor"
                ? $"+++> {@class}({string.Join(", ", arguments)}) constructor called"
                : $"+++> {@class}.{callingMethod}({string.Join(", ", arguments)}) called";

            return output;
        }

        public static string DateStamp => DateTime.Now.ToString(DateFormat);

        public static string DateTimeStamp => DateTime.Now.ToString(FileStampFormat);

        public static string TruncateString(this string str, int maxLength)
        {
            return str.Substring(0, Math.Min(str.Length, maxLength));
        }

        public static T CastType<T>(object input)
        {
            return (T)input;
        }

        public static T ConvertType<T>(object input)
        {
            return (T)Convert.ChangeType(input, typeof(T));
        }

        public static DateTime AddWeeks(this DateTime dateTime, int weeks)
        {
            return dateTime.AddDays(weeks * 7);
        }

        public static DateTime AddQuarters(this DateTime dateTime, int quarters)
        {
            return dateTime.AddMonths(quarters * 3);
        }

        #region Object enchantments

        //Additional methods for object
        //public static bool HasValue(this object o) { return !(o is DBNull || o != null); }
        //public static bool HasNoValue(this object o) { return o is DBNull || o == null; }

        public static T1 CopyFrom<T1, T2>(this T1 obj, T2 otherObject)
            where T1 : class
            where T2 : class
        {
            var srcFields = otherObject.GetType().GetProperties(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty);

            var destFields = obj.GetType().GetProperties(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);

            foreach (var property in srcFields)
            {
                var dest = destFields.FirstOrDefault(x => x.Name == property.Name);
                if (dest != null && dest.CanWrite)
                    dest.SetValue(obj, property.GetValue(otherObject, null), null);
            }

            return obj;
        }

        #endregion Object enchantments

        #region byte/color/bitmap/file/image/stream enchantments


        /*
        public static Color ToMediaColor(this System.Drawing.Color color)
        {
            return Color.FromArgb(color.A, color.R, color.G, color.B);
            //usage:
            //var ColorFromHTML = new SolidColorBrush(System.Drawing.ColorTranslator.FromHtml("#F4F4F4").ToMediaColor());
        }
        */

        public static void ToFile(this byte[] byteArray, string fileName)
        {
            try
            {
                var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                fileStream.Write(byteArray, 0, byteArray.Length);
                fileStream.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error", $"Problem with saving file: {fileName}\n{ex.Messages()}");
            }
        }

        public static IEnumerable<string> FromFile(string filePath)
        {
            var source = File.ReadAllLines(filePath);
            var result = source.Where(s => s.Trim().Length >= 1).ToList();
            return result;
            //var item = (s.Split('=')[0], s.Split('=')[1]);
        }

        public static ImageSource ToImage(this byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return new BitmapImage(new Uri(@"Resources\image-not-found.png", UriKind.Relative));

            var biImg = new BitmapImage();
            biImg.BeginInit();
            biImg.StreamSource = new MemoryStream(imageData);
            biImg.EndInit();
            return biImg;
        }

        public static string ToByte(this FileStream fs)
        {
            var imgBytes = new byte[fs.Length];
            fs.Read(imgBytes, 0, Convert.ToInt32(fs.Length));
            var encodeData = Convert.ToBase64String(imgBytes, Base64FormattingOptions.InsertLineBreaks);
            return encodeData;
        }

        #endregion byte/bitmap/image/stream enchantments

        #region RichTextBox enchantments

        public static string Text(this RichTextBox rtb)
        {
            return new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd).Text;
        }

        public static void Text(this RichTextBox rtb, string txt)
        {
            rtb.AppendText(txt);
        }

        #endregion RichTextBox enchantements

        #region Compare two DataTables and return a DataTable with DifferentRecords

        /// <summary>  
        /// Compare two DataTables and return a DataTable with DifferentRecords  
        /// </summary>  
        /// <param name="firstDataTable">FirstDataTable</param>  
        /// <param name="secondDataTable">SecondDataTable</param>  
        /// <returns>DifferentRecords</returns>  
        public static DataTable CompareWith(this DataTable firstDataTable, DataTable secondDataTable)
        {
            //Create Empty Table  
            var resultDataTable = new DataTable("ResultDataTable");

            //use a Dataset to make use of a DataRelation object  
            using (var ds = new DataSet())
            {
                //Add tables with alternating their names 
                var t1 = firstDataTable.Copy();
                t1.TableName = "1";

                var t2 = secondDataTable.Copy();
                t2.TableName = "2";

                ds.Tables.AddRange(new[] { t1, t2 });

                //Get Columns for DataRelation  
                var firstColumns = new DataColumn[ds.Tables[0].Columns.Count];
                for (var i = 0; i < firstColumns.Length; i++)
                {
                    firstColumns[i] = ds.Tables[0].Columns[i];
                }

                var secondColumns = new DataColumn[ds.Tables[1].Columns.Count];
                for (var i = 0; i < secondColumns.Length; i++)
                {
                    secondColumns[i] = ds.Tables[1].Columns[i];
                }

                //Create DataRelation  
                var r1 = new DataRelation(string.Empty, firstColumns, secondColumns, false);
                ds.Relations.Add(r1);

                var r2 = new DataRelation(string.Empty, secondColumns, firstColumns, false);
                ds.Relations.Add(r2);

                //Create columns for return table  
                for (var i = 0; i < firstDataTable.Columns.Count; i++)
                {
                    resultDataTable.Columns.Add(firstDataTable.Columns[i].ColumnName, firstDataTable.Columns[i].DataType);
                }

                //If FirstDataTable Row not in SecondDataTable, Add to ResultDataTable.  
                resultDataTable.BeginLoadData();
                foreach (DataRow parentrow in ds.Tables[0].Rows)
                {
                    var childrows = parentrow.GetChildRows(r1);
                    if (childrows.Length == 0)
                        resultDataTable.LoadDataRow(parentrow.ItemArray, true);
                }

                //If SecondDataTable Row not in FirstDataTable, Add to ResultDataTable.  
                foreach (DataRow parentrow in ds.Tables[1].Rows)
                {
                    var childrows = parentrow.GetChildRows(r2);
                    if (childrows.Length == 0)
                        resultDataTable.LoadDataRow(parentrow.ItemArray, true);
                }
                resultDataTable.EndLoadData();
            }

            return resultDataTable;
        }
        #endregion

        #region DataRow enchantments

        public static bool? ToBool(this DataRow row, string columnName)
        {
            if (HasNoColumn(row, columnName) || row[columnName] is DBNull)
            {
                return null;
            }

            switch (row[columnName].ToString().ToLower().Trim())
            {
                case "true":
                case "yes":
                case "1":
                    return true;

                case "false":
                case "no":
                case "0":
                    return false;

                default:
                    return null;
            }
        }

        public static int? ToInt(this DataRow row, string columnName)
        {
            if (HasNoColumn(row, columnName) || row[columnName] is DBNull)
            {
                return null;
            }

            int? value;
            try
            {
                value = Convert.ToInt32(row[columnName].ToString());
            }
            catch
            {
                MessageBox.Show($"To Int conversion problem with {columnName} of value = {row[columnName]}");
                value = null;
            }
            return value;
        }

        public static string ToString(this DataRow row, string columnName)
        {
            if (HasNoColumn(row, columnName) || row[columnName] is DBNull)
            {
                return null;
            }

            return row[columnName].ToString();
        }

        public static T? ToEnum<T>(this DataRow row, string columnName) where T : struct
        {
            if (HasNoColumn(row, columnName) || row[columnName] is DBNull)
            {
                return null;
            }

            T result;

            var input = row[columnName].ToString();

            var isConvertable = Enum.TryParse(input, true, out result);

            if (isConvertable == false)
            {
                MessageBox.Show($"Problems with conversion from {columnName} to {typeof(T)}");
                return null;
            }

            return result;
        }

        public static bool In<T>(this T testedEntry, IEnumerable<T> collection) where T : struct
        {
            return collection.Contains(testedEntry);
        }


        public static bool HasColumn(this DataRow row, string columnName)
        {
            return row?.Table != null && row.Table.Columns.Contains(columnName);
        }

        public static bool HasNoColumn(this DataRow row, string columnName)
        {
            return HasColumn(row, columnName) == false;
        }

        #endregion DataRow enchantements

        #region IEnumerable enchantments

        /*
        //jp: Not sure if this is better than next, more straight forward function.
        public static ObservableCollection<T> ToCollection<T>(this IEnumerable<T> source) where T : class
        {
            var collection = new ObservableCollection<T>();
            foreach (T unknown in source)
            {
                collection.Add(unknown);
            }

            return collection;
        }
        */

        public static T NextOf<T>(this IList<T> list, T item)
        {
            return list[(list.IndexOf(item) + 1) == list.Count ? 0 : (list.IndexOf(item) + 1)];
        }


        /// <summary>
        /// Copy collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static ObservableCollection<T> ToCollection<T>(this IEnumerable<T> source)
        {
            return new ObservableCollection<T>(source);
        }

        public static bool NotContains(this IEnumerable<string> list, string value)
        {
            return !list.Contains(value);
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            var knownKeys = new HashSet<TKey>();
            return source.Where(element => knownKeys.Add(keySelector(element)));
        }

        public static bool None<TSource>(this IEnumerable<TSource> source)
        {
            bool result;
            if (source == null)
            {
                result = true;
            }
            else
            {
                result = source.Any() == false;
            }
            return result;
        }

        public static bool Many(this IEnumerable<object> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source.Count() > 1;
        }

        public static bool Many<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            var counter = 0;

            foreach (var source1 in source)
            {
                if (predicate(source1)) counter++;

                if (counter > 1) return true;
            }

            return false;
        }

        public static bool None<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            bool result;
            if (source == null)
            {
                result = true;
            }
            else
            {
                result = source.Any(predicate) == false;
            }
            return result;
        }

        #endregion IEnumerable enchantements

        public static string ToStringJoinedBy(this Dictionary<string, string> d, string separator)
        {
            return d.Keys.Aggregate("", (current, item) => current + (item + " \t" + d[item] + separator));
        }

        public static XElement ToXML(this Dictionary<string, string> d)
        {
            return new XElement("root", d.Select(kv => new XElement(kv.Key, kv.Value)));
        }

        public static void FromXML(this Dictionary<string, string> d, XmlDocument xml)
        {
            d.Clear();
            var x2 = xml.DocumentElement;
            if (x2 == null) return;
            foreach (XmlNode el in x2.ChildNodes)
            {
                d.Add(el.Name, el.FirstChild.Value);
            }
        }

        public static string Directory(this Window w)
        {
            return AppDomain.CurrentDomain.BaseDirectory;
            //return AppDomain.CurrentDomain.BaseDirectory;
        }

        public static string Executable(this Window w)
        {
            return w.GetType().Assembly.Location;
            //return Assembly.GetEntryAssembly().Location;
            //return Assembly.GetExecutingAssembly().Location;
        }

        public static string GetVersion()
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(executingAssembly.Location);
            var result = $"{fileVersionInfo.FileMajorPart}.{fileVersionInfo.FileMinorPart}.{fileVersionInfo.FileBuildPart} ({fileVersionInfo.ProductVersion})";
            return result;
        }

        public static string Created(this Window w)
        {
            return File.GetLastWriteTime(w.Executable()).ToString(DateTimeFormat);
        }


        #region ItemsControl enchantments

        public static bool ShowTab(this ItemsControl host, string tabname)
        {
            if (host == null) return false;

            foreach (TabItem tab in host.Items)
            {
                if (tab.Name == tabname)
                {
                    tab.IsSelected = true;
                    return true;
                }
            }
            return false;
        }

        #endregion ItemsControl enchantments

        #region Exception enchantments

        public static string Messages(this Exception exception)
        {
            var result = "";

            while (exception != null)
            {
                result = result.HasNoValue()
                    ? exception.Message
                    : result + "\n" + exception.Message;

                exception = exception.InnerException;
            }

            return result;
        }

        #endregion Exception enchantments

        #region String enchantments

        public static bool ParseDate(string value, out DateTime dateValue)
        {

            if (DateTime.TryParse(value, out dateValue))
                return true;

            if (ParseDate(
                value,
                new[] { "dd/MM/yyyy", "yyyy-MM-dd", "dd-MM-yyyy" },
                out dateValue))
                return true;

            if (ParseDate(
                value.JustDigits(),
                new[] { "ddMMyy", "yyMMdd", "yyyyMMdd", "ddMMyyyy" },
                out dateValue
                ))
                return true;

            return false;
        }

        public static bool ParseDate(string value, IEnumerable<string> formatArr, out DateTime dateValue)
        {
            foreach (var format in formatArr)
            {
                if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateValue))
                {
                    return true;
                }
            }
            dateValue = new DateTime(1, 1, 1);
            return false;
        }

        public static string FormatDateOrTime(string format, string input)
        {
            if (input.HasNoValue()) return input;

            DateTime output;

            //input can come from control rather than database so additional formats are needed (according to default configuration of date picker)
            var formats = new List<string> { format, DateTimeFormat, "dd/MM/yyyy", "dd/MM/yyyy HH:mm:ss", "HH:mm:ss" };

            var test = ParseDate(input, formats, out output);
            if (test)
                return output.ToString(format);

            //if it was not parsed successfully than try other ways...
            var digitsCount = input.Count(char.IsDigit);
            var expectedCount = 0;
            switch (format)
            {
                case TimeFormat: expectedCount = 6; break;
                case DateFormat: expectedCount = 8; break;
                case DateTimeFormat: expectedCount = 14; break;
            }

            if (digitsCount < expectedCount)
            {
                return input;
            }

            throw new NotSupportedException($"Date/Time Input format ({format}) not supported for this value: {input}");
        }

        public static string CleanLineBreaks(this string value)
        {
            value = value.Replace("\n", " ").Replace("\r", " ").Replace("\t", " ");

            return value;
        }

        public static string CleanMultipleSpaces(this string value)
        {
            var options = RegexOptions.None;
            var regex = new Regex(@"[ ]{2,}", options);
            value = regex.Replace(value, @" ");

            return value;
        }

        public static bool IsFirst(this IEnumerable<object> list, object value)
        {
            var r = list.First();
            return value == r;
        }

        public static bool IsLast(this IEnumerable<object> list, object value)
        {
            var r = list.Last();
            return value == r;
        }

        public static bool IsNotLast(this IEnumerable<object> list, object value)
        {
            var r = list.Last();
            return value != r;
        }

        public static bool In(this string s, IEnumerable<string> v)
        {
            return v.Contains(s);
        }

        public static bool NotIn(this string s, IEnumerable<string> v)
        {
            return !s.In(v);
        }

        public static bool In(this char c, string s)
        {
            var t = s.Contains(c);
            return t;
        }

        public static bool NotIn(this char c, string s)
        {
            return !c.In(s);
        }

        public static string GetSubstringBetween(this string from, string after, string before, StringComparison comparison = StringComparison.InvariantCultureIgnoreCase)
        {
            return @from.Substring(
                @from.IndexOf(after, comparison) + after.Length,
                @from.IndexOf(before, comparison) - @from.IndexOf(after, comparison) - after.Length
            );
        }

        public static string ToDateString(this string value)
        {
            return FormatDateOrTime(DateFormat, value);
        }

        public static string ToTimeString(this string value)
        {
            return FormatDateOrTime(TimeFormat, value);
        }

        public static string Unquote(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            var length = value.Length;
            if (length < 2) return value;

            var fc = value[0];
            var lc = value[length - 1];

            if (fc == '\"' && lc == '\"')
            {
                value = value.Substring(1, length - 2);
            }
            else if (fc == '\'' && lc == '\'')
            {
                value = value.Substring(1, length - 2);
            }

            //fix for escaping problems with stored procedures 
            value = value.Replace(@"\'", @"'");
            value = value.Replace(@"\`", @"`");
            value = value.Replace(@"\""", @"""");
            return value;
        }

        [ContractAnnotation("null=>false")]
        public static bool HasValue(this double? d)
        {
            return d.HasValue;
        }

        [ContractAnnotation("null=>false")]
        public static bool HasValue(this int? i)
        {
            return i.HasValue;
        }

        [ContractAnnotation("null=>false")]
        public static bool HasValue(this DateTime? d)
        {
            return d.HasValue;
        }

        [ContractAnnotation("null=>false")]
        public static bool HasValue(this TimeSpan? d)
        {
            return d.HasValue;
        }

        [ContractAnnotation("null=>false")]
        public static bool HasValue(this string s)
        {
            return !string.IsNullOrWhiteSpace(s);
        }

        [ContractAnnotation("null=>true")]
        public static bool HasNoValue(this double? value)
        {
            return !value.HasValue();
        }

        [ContractAnnotation("null=>true")]
        public static bool HasNoValue(this int? value)
        {
            return !value.HasValue();
        }

        [ContractAnnotation("null=>true")]
        public static bool HasNoValue(this DateTime? value)
        {
            return !value.HasValue();
        }

        [ContractAnnotation("null=>true")]
        public static bool HasNoValue(this TimeSpan? value)
        {
            return !value.HasValue();
        }

        [ContractAnnotation("null=>true")]
        public static bool HasNoValue(this string value)
        {
            return !value.HasValue();
        }

        public static string RemoveWhitespaces(this string input)
        {
            return new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray());
        }

        public static bool Contains(this string source, string value, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return source?.IndexOf(value, comparison) >= 0;
        }

        public static bool ContainsAny(this string source, IEnumerable<string> values, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return values.Any(value => source.Contains(value, comparison));
        }

        public static bool NotEquals(this string source, string value, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return !source.Equals(value, comparison);
        }

        public static bool NotContains(this string source, string value, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return !source.Contains(value, comparison);
        }

        public static T Next<T>(this T src) where T : struct
        {
            if (!typeof(T).IsEnum) throw new ArgumentException($"Argument {typeof(T).FullName} is not an Enum");

            var values = (T[])Enum.GetValues(src.GetType());
            var index = Array.IndexOf(values, src) + 1;
            return values.Length == index ? values[0] : values[index];
        }

        public static T? ToEnum<T>(this string value, bool ignoreCase = true)
            where T : struct, IComparable, IFormattable, IConvertible
        {
            if (!typeof(T).IsEnum) throw new ArgumentException($"Argument {typeof(T).FullName} is not an Enum");
            if (value.HasNoValue()) return null;

            var result = Enum.TryParse(value, ignoreCase, out T lResult);

            return result ? (T?)lResult : null;
        }

        public static T ToEnum<T>(this string value, T defaultValue, bool ignoreCase = true)
            where T : struct, IComparable, IFormattable, IConvertible
        {
            if (!typeof(T).IsEnum) throw new ArgumentException($"Argument {typeof(T).FullName} is not an Enum");
            if (value.HasNoValue()) return defaultValue;

            var result = Enum.TryParse(value, ignoreCase, out T lResult);

            return result ? lResult : defaultValue;
        }

        public static int? ToInt(this string s, bool showWarning = true)
        {
            if (s.HasNoValue()) return null;

            int result;
            var isValid = int.TryParse(s, out result);

            if (!isValid && showWarning)
            {
                MessageBox.Show($"Error parsing string to int\nOriginal value = '{s}'\nReturned value = 'result'");
            }

            return (isValid) ? result : (int?)null;
        }

        public static double? ToDouble(this string s, bool showWarning = true)
        {
            if (s.HasNoValue()) return null;

            double result;
            var isValid = double.TryParse(s, out result);

            if (!isValid && showWarning)
            {
                MessageBox.Show($"Error parsing string to double!\nOriginal value = '{s}'\nReturned value = 'result'");
            }

            return (isValid) ? result : (double?)null;
        }

        public static DateTime? ToDate(this string s, bool showWarning = true)
        {
            DateTime result;
            if (s.HasNoValue()) return null;

            bool isValid;

            if (s.ToUpper() == "NOW")
            {
                result = DateTime.Now;
                isValid = true;
            }
            else
            {
                isValid = DateTime.TryParse(s, out result);

                if (!isValid && showWarning)
                {
                    MessageBox.Show($"Error parsing string to date\nOriginal value = '{s}'\nReturned value = '{result}'");
                }
            }

            return (isValid) ? result : (DateTime?)null;
        }

        public static TimeSpan? ToTime(this string s, bool showWarning = true)
        {
            TimeSpan result;
            if (s.HasNoValue()) return null;

            bool isValid;

            if (s.ToUpper() == "NOW")
            {
                result = DateTime.Now.TimeOfDay;
                isValid = true;
            }
            else
            {
                isValid = TimeSpan.TryParse(s, out result);

                if (!isValid && showWarning)
                {
                    MessageBox.Show($"Error parsing string to time\nOriginal value = '{s}'\nReturned value = 'result'");
                }
            }

            return (isValid) ? result : (TimeSpan?)null;
        }

        public static string Encrypt(this string s, bool reversible = true)
        {
            string result;
            if (reversible)
            {
                //encryption   
                var readChar = s.ToCharArray();
                result = readChar
                    .Select(t => Convert.ToInt32(t) + 10)
                    .Select(no => "" + Convert.ToChar(no))
                    .Aggregate("", (current, r) => current + r);
            }
            else
            {
                result = RandomString(s.Length, s.Length).First();
            }

            return result;
        }

        public static string Hash(this string s)
        {
            // step 1, calculate MD5 hash from input
            var md5 = MD5.Create();
            var inputBytes = Encoding.ASCII.GetBytes(s);
            var hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            var sb = new StringBuilder();
            foreach (var t in hash)
            {
                sb.Append(t.ToString("X2"));
            }
            return sb.ToString();
        }

        public static string Decrypt(this string s)
        {
            //decryption  
            var readChar = s.ToCharArray();
            return readChar.Select(t => Convert.ToInt32(t) - 10).Select(no => "" + Convert.ToChar(no)).Aggregate("", (current, r) => current + r);
        }

        private static IEnumerable<string> RandomString(int minLength, int maxLength)
        {
            var chars = new char[maxLength];
            var setLength = RandomChars.Length;

            var length = Rng.Next(minLength, maxLength + 1);

            for (var i = 0; i < length; ++i)
            {
                chars[i] = RandomChars[Rng.Next(setLength)];
            }

            yield return new string(chars, 0, length);
        }

        public static string NullIfEmpty(this string s)
        {
            return s.Trim().HasNoValue() ? null : s;
        }

        public static string ToTitleCase(this string s)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.ToLower());
        }

        public static string JustDigits(this string s)
        {
            var n = 0;
            var result = string.Empty;
            return s.Where(c => int.TryParse("" + c, out n)).Aggregate(result, (current, c) => current + n);
        }

        public static bool ContainsAnyLetter(this string s)
        {
            return s.Any(char.IsLetter);
        }

        public static bool ToFile(this string s, string filename, bool append)
        {
            using (var f = new StreamWriter(filename, append))
            {
                f.Write(s);
            }
            return File.Exists(filename);
        }

        public static string FromFile(this string s, string filename)
        {
            return new StreamReader(filename).ReadToEnd();
        }

        // String-formatted percentage for display purposes
        public static string ToPercentageOf(this string subtotalValue, string totalValue, int roundDPs = 0)
        {
            var subDouble = Convert.ToDouble(subtotalValue);
            var totalDouble = Convert.ToDouble(totalValue);

            if (totalDouble <= 0) return "-";
            var unroundedPercentage = (subDouble / totalDouble) * 100;
            var roundedPercentage = Math.Round(unroundedPercentage, roundDPs);
            return roundedPercentage + "%";
        }

        public static char ToUpper(this char character)
        {
            return char.ToUpper(character);
        }

        public static bool IsValidCode([NotNull] string code)
        {
            if (code.HasNoValue()) return false;

            var id = code.JustDigits();

            if (id.HasNoValue()) return false;

            //split check based on code with checksum or not
            if (code.ContainsAnyLetter())
            {
                switch (code.First())
                {
                    //todo: implement code A validation, details are unknown
                    case 'A': return true;

                    case 'R': return IsModuloCorrect(code);

                    //s number is compiled from 'S' + studyNumber.LeftPad(3,0) +  proper code, so we are checking just last part
                    case 'S': return IsModuloCorrect(code.Substring(4));
                }
            }
            else
            {
                if (code.First().NotIn("79")) return false;

                return code.Length == 6;
            }

            return false;
        }

        private static bool IsModuloCorrect(this string code)
        {
            //Note: anon_study_participation_id is compiled from 'S' + studyNumber.LeftPad(3,0) + code

            var checksum = (code.JustDigits().ToInt() % 23);

            var checkLetter = '\0';
            switch (checksum)
            {
                case 0: checkLetter = 'Z'; break;
                case 1: checkLetter = 'A'; break;
                case 2: checkLetter = 'B'; break;
                case 3: checkLetter = 'C'; break;
                case 4: checkLetter = 'D'; break;
                case 5: checkLetter = 'E'; break;
                case 6: checkLetter = 'F'; break;
                case 7: checkLetter = 'G'; break;
                case 8: checkLetter = 'H'; break;
                case 9: checkLetter = 'J'; break;
                case 10: checkLetter = 'K'; break;
                case 11: checkLetter = 'L'; break;
                case 12: checkLetter = 'M'; break;
                case 13: checkLetter = 'N'; break;
                case 14: checkLetter = 'P'; break;
                case 15: checkLetter = 'Q'; break;
                case 16: checkLetter = 'R'; break;
                case 17: checkLetter = 'S'; break;
                case 18: checkLetter = 'T'; break;
                case 19: checkLetter = 'V'; break;
                case 20: checkLetter = 'W'; break;
                case 21: checkLetter = 'X'; break;
                case 22: checkLetter = 'Y'; break;
            }

            return code.Last().ToUpper() == checkLetter;
        }

        #endregion

        #region Array enchantments

        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            var result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        #endregion

        #region Reflections

        public static string GetAssemblyName => Assembly.GetEntryAssembly().GetName().Name;

        public static string GetAssemblyVersion => Assembly.GetEntryAssembly().GetName().Version.ToString();

        public static bool TrySetProperty<TValue>(this object obj, string propertyName, TValue value)
        {
            var property = obj.GetType().GetProperties()
                .Where(p => p.CanWrite && p.PropertyType == typeof(TValue))
                .FirstOrDefault(p => p.Name == propertyName);

            if (property == null) return false;

            //todo not sure if it works, was without null in source
            property.SetValue(obj, value, null);
            return true;
        }

        public static TProperty GetPropertyValue<TProperty>(this object obj, string propertyName)
        {
            var property = obj.GetType().GetProperties()
                .Where(p => p.CanRead && p.PropertyType == typeof(TProperty))
                .FirstOrDefault(p => p.Name == propertyName);

            if (property == null) return default(TProperty);

            //todo not sure if it works, was without null in source
            return (TProperty)property.GetValue(obj, null);
        }

        #endregion

        #region UI enchantments

        public static T FindAncestor<T>(DependencyObject obj) where T : DependencyObject
        {
            while (obj != null)
            {
                var o = obj as T;
                if (o != null)
                {
                    return o;
                }
                obj = VisualTreeHelper.GetParent(obj);
            }
            return null;
        }

        #endregion UI enchantments

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, ref Rect lpRect);

        /*
        [DllImport("gdi32.dll")]
        private static extern int GetBoundsRect(IntPtr hWnd, ref Rect lpRect, int flags = 0);
        */

        /*
        [DllImport("gdi32.dll")]
        private static extern int GetClipBox(IntPtr hWnd, ref Rect lpRect);
        */

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public readonly int Left;
            public readonly int Top;
            public readonly int Right;
            public readonly int Bottom;
        }

        /*
        public static Rectangle GetWindowBounds()
        {
            var hwnd = Process.GetCurrentProcess().MainWindowHandle;
            var rectangle = new Rect();
            var success = GetWindowRect(hwnd, ref rectangle);
            //todo: this two does not work - problem not solved - find only visible part of window
            //var success = GetBoundsRect(hwnd, ref rectangle) != -1;
            //var success = GetClipBox(hwnd, ref rectangle) != -1;

            Rectangle result;

            if (success)
            {
                //-8 if lower than 0 as fullscreen app has coords starting from -8,-8 and than it is +8 too wide and to high
                result = new Rectangle(
                    rectangle.Left,
                    rectangle.Top,
                    (rectangle.Left > 0) ? rectangle.Right - rectangle.Left : rectangle.Right - 8,
                    (rectangle.Top > 0) ? rectangle.Bottom - rectangle.Top : rectangle.Bottom - 8
                    );


            }
            else
            {
                result = new Rectangle(0, 0, 10, 10);
            }
            return result;
        }

        public static Point GetWindowMousePosition()
        {
            var w = GetWindowBounds();
            var l = Cursor.Position;
            if (w.X > 0) l.X = l.X - w.X;
            if (w.Y > 0) l.Y = l.Y - w.Y;
            return l;
        }
        */

        public static string HostName => Environment.MachineName.ToUpper();



        //defines if application is running from VisualStudio (in most cases)
        public static bool IsDebuggerAttached => Debugger.IsAttached;

        //defines if application is not running (methods can be triggered from visualstudio xaml designer itself)
        public static bool InDesignMode => DesignerProperties.GetIsInDesignMode(new DependencyObject());

        public static bool IsMainThread => (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA);
        //(Application.Current != null && Application.Current.Dispatcher.Thread == Thread.CurrentThread);


        private static string _currentUserName;

        public static string CurrentUserName
        {
            set
            {
                //can be set only once, allows to run application as different user
                if (_currentUserName == null)
                {
                    _currentUserName = value;
                }
                else
                {
                    var ex = new Exception("Username already set!");
                    MessageBox.Show(ex.Messages());
                    throw ex;
                }
            }
            get
            {
                return _currentUserName ?? Environment.UserName.ToLower();
            }
        }
        /*
        public static string GetUNCPath(this DriveInfo drive)
        {
            var path = (drive.RootDirectory).FullName.Substring(0, 2);
            if (path.StartsWith(@"\\")) return path;

            var mo = new ManagementObject
            {
                Path = new ManagementPath($"Win32_LogicalDisk='{path}'")
            };

            //DriveType 4 = Network Drive
            return Convert.ToUInt32(mo["DriveType"]) == 4
                       ? Convert.ToString(mo["ProviderName"])
                       : path;
        }
        */
        public static int LaunchApplication(string executable, string arguments, string homeDir, bool visible = true, bool waitForExit = false, string appDomain = null)
        {
            if (!File.Exists(executable)) return 0;

            // ReSharper disable RedundantIfElseBlock
            if (waitForExit)
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    WorkingDirectory = homeDir,
                    Arguments = arguments,
                    FileName = executable,
                    WindowStyle = (visible) ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
                    CreateNoWindow = !visible,
                    Domain = appDomain
                });
                process?.WaitForExit();
                return 0;
            }
            else
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    WorkingDirectory = homeDir,
                    Arguments = arguments,
                    FileName = executable,
                    WindowStyle = (visible) ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
                    CreateNoWindow = !visible,
                    Domain = appDomain
                });

                if (process != null)
                {
                    return process.Id;
                }
                else
                {
                    MessageBox.Show($"Something went wrong while starting {executable} file.");
                    return 0;
                }
            }
            // ReSharper restore RedundantIfElseBlock
        }

        public static void LogOff()
        {
            try
            {
                LaunchApplication(
                    @"C:\Windows\System32\logoff.exe",
                    "",
                    "",
                    false
                    );
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Messages());
                throw;
            }
        }

        public static string AspectRatio(ImageSource i)
        {
            var w = (int)i.Width;
            var h = (int)i.Height;

            if (w == 640 && h == 480) return "VGA	4:3";
            if (w == 768 && h == 768) return "3:4";

            if (w == 800 && h == 600) return "SVGA 4:3";
            if (w == 1024 && h == 1024) return "XGA 4:3";
            if (w == 1024 && h == 600) return "WSVGA ~17:10";

            if (w == 1093 && h == 614) return "16:09";
            if (w == 1152 && h == 864) return "XGA+ 4:3";
            if (w == 1280 && h == 800) return "WXGA 16:10";
            if (w == 1280 && h == 720) return "WXGA 16:9";
            if (w == 1280 && h == 960) return "SXGA–(UVGA)	4:3";
            if (w == 1280 && h == 768) return "WXGA 5:3";
            if (w == 1280 && h == 1024) return "SXGA 5:4";

            if (w == 1311 && h == 737) return "~16:9";
            if (w == 1360 && h == 768) return "HD ~16:9";
            if (w == 1366 && h == 768) return "HD ~16:9";
            if (w == 1400 && h == 1050) return "SXGA+ 4:3";
            if (w == 1440 && h == 900) return "WXGA+ 16:10";
            if (w == 1600 && h == 900) return "HD+ 16:9";
            if (w == 1600 && h == 1200) return "UXGA 4:3";
            if (w == 1680 && h == 1050) return "WSXGA+ 16:10";

            if (w == 1920 && h == 1200) return "WUXGA 16:10";
            if (w == 1920 && h == 1080) return "FHD 16:9";
            if (w == 2048 && h == 1152) return "QWXGA 16:9";
            if (w == 2560 && h == 1600) return "WQXGA 16:10";
            if (w == 2560 && h == 1440) return "WQHD 16:9";

            return "Unknown!";
        }

        public static int GreatestCommonDivisor(int a, int b)
        {
            while (b != 0)
            {
                var remainder = a % b;
                a = b;
                b = remainder;
            }
            return a;
        }

        public static void Sleep(int seconds)
        {
            Thread.Sleep(seconds * 1000);
        }

        public static void DoEvents()
        {
            if (Application.Current != null) Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
        }

        public static bool ConfirmPrevent(string message = null)
        {
            if (message.HasNoValue()) message = "Do you want to go back and save the changes you made?";
            var result = MessageBox.Show(message, GetAssemblyName, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }


        public static bool ConfirmSave(string message = null)
        {
            if (message.HasNoValue()) message = "Do you want to save changes you made?";
            var result = MessageBox.Show(message, GetAssemblyName, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        public static bool ConfirmDelete(string message = null, string entityName = null)
        {
            string entityText = "";

            if (entityName.HasValue()) entityText = " this " + entityName;

            if (message.HasNoValue()) message = "Are you sure you want to delete" + entityText + "?";

            var result = MessageBox.Show(message, GetAssemblyName, MessageBoxButton.YesNo, MessageBoxImage.Stop);
            return result == MessageBoxResult.Yes;
        }

        public static bool EvaluateFilter(string filter, IEnumerable<string> scope)
        {
            if (filter.HasNoValue()) return true;

            var list = scope?.ToList();
            if (list.None())
            {
                Debug.WriteLine($"Warning: Null or empty scope was provided for filtering on {filter}");
                return true;
            }

            var result = false;

            var andFilter = filter.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);

            var orFilter = filter.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            filter = filter.Replace("&", "").Replace("|", "");

            if (andFilter.Length > 1)
            {
                var matchesCount = 0;
                foreach (var entry in andFilter)
                {
                    result = list.Any(p => p.Contains(entry, StringComparison.OrdinalIgnoreCase));
                    if (result) matchesCount++;
                }

                result = matchesCount == andFilter.Length;
            }
            else if (orFilter.Length > 1)
            {
                foreach (var entry in orFilter)
                {
                    result = list.Any(p => p.Contains(entry, StringComparison.OrdinalIgnoreCase));
                    if (result) break;
                }
            }
            else
            {
                result = list.Any(p => p.Contains(filter, StringComparison.OrdinalIgnoreCase));
            }

            return result;
        }

        public static string MakeFileNameCompliant(this string phrase)
        {
            if (phrase == null) return null;
            var fileName = phrase;
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }
    }
}

