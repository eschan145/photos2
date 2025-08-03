using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

using WinRT.Interop;

using ImageMagick;

enum DataType
{
    STRING,
    MULTILINESTRING,
    DATE,
    MULTISTRING,
    MULTILINE,
    LOCATION
}

namespace photos.Views
{
    public partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            string path = @"test.jpg";
            Dictionary<string, string> metadata = ExtractMetadata(path);
            metadata.TryGetValue("XPTitle", out var title);

            using var image = new MagickImage(path);
            var exif = image.GetExifProfile();

            string focalLength = "Unknown";
            string aperture = "Unknown";
            string exposure = "Unknown";
            string iso = "Unknown";

            if (exif == null)
                throw new Exception("Empty exif data!");

            var rawFocal = exif.GetValue(ExifTag.FocalLength);
            if (rawFocal != null)
            {
                var focal = ParseRational(rawFocal.ToString());
                if (focal != null)
                    focalLength = $"{focal:0.##}mm";
            }

            var rawFNumber = exif.GetValue(ExifTag.FNumber);
            if (rawFNumber != null)
            {
                var fstop = ParseRational(rawFNumber.ToString());
                if (fstop != null)
                    aperture = $"f/{fstop:0.0}";
            }

            var rawExposure = exif.GetValue(ExifTag.ExposureTime);
            if (rawExposure != null)
            {
                var parts = rawExposure.ToString().Split('/');
                if (parts.Length == 2 &&
                    double.TryParse(parts[0], out double num) &&
                    double.TryParse(parts[1], out double den) &&
                    den != 0)
                {
                    if (num == 1)
                        exposure = $"1/{(int)den} sec";
                    else
                        exposure = $"{num / den:0.###} sec";
                }
                else
                {
                    exposure = rawExposure.ToString();
                }
            }

            var rawIso = exif.GetValue(ExifTag.ISOSpeedRatings);
            if (rawIso != null)
            {
                iso = $"ISO {rawIso.Value[0]}";
            }

            var rawMake = exif.GetValue(ExifTag.Make);
            var rawModel = exif.GetValue(ExifTag.Model);

            string makeStr = rawMake?.ToString().Trim() ?? "Unknown";
            string modelStr = rawModel?.ToString().Trim() ?? "Unknown";

            var dateTag = exif.GetValue(ExifTag.DateTimeOriginal) ??
                          exif.GetValue(ExifTag.DateTime) ??
                          exif.GetValue(ExifTag.DateTimeDigitized);

            string dateString = dateTag.Value;
            string date = "";

            if (DateTime.TryParseExact(dateString, "yyyy:MM:dd HH:mm:ss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime rawDate))
            {
                date = rawDate.ToString("yyyy-MM-dd");
            }

            double dpiX = image.Density.X;
            double dpiY = image.Density.Y;
            if (image.Density.Units == DensityUnit.PixelsPerCentimeter)
            {
                dpiX *= 2.54;
                dpiY *= 2.54;
            }
            double dpi = (dpiX + dpiY) / 2.0;

            this.CreateWidgets("Title", DataType.STRING, "title", new List<string> { title });
            this.CreateWidgets("Description", DataType.MULTILINESTRING, "description");
            this.CreateWidgets("Date", DataType.DATE, "date", new List<string> { date });
            this.CreateWidgets("Size info", DataType.MULTISTRING, "dimension", new List<string>
            {
                $"{image.Width} x {image.Height}",
                FormatSize(new FileInfo(path).Length),
                dpi.ToString("F1") + " dpi"
            });
            this.CreateWidgets("Camera", DataType.MULTISTRING, "camera", new List<string>
            {
                makeStr,
                modelStr,
                focalLength,
                aperture,
                exposure,
                iso
            });
        }

        public static double? ParseRational(string rational)
        {
            if (string.IsNullOrWhiteSpace(rational))
                return null;

            var parts = rational.Split('/');
            if (parts.Length != 2)
                return null;

            if (double.TryParse(parts[0], out double numerator) &&
                double.TryParse(parts[1], out double denominator) &&
                denominator != 0)
            {
                return numerator / denominator;
            }

            return null;
        }

        string FormatSize(long bytes)
        {
            double size = bytes;
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            int index = 0;

            while (size >= 1024.0 && index < units.Length - 1)
            {
                size /= 1024.0;
                ++index;
            }

            int precision = size < 10.0 ? 1 : 0;
            return size.ToString($"F{precision}") + " " + units[index];
        }

        public static Dictionary<string, string> ExtractMetadata(string path)
        {
            using var image = new MagickImage(path);
            var profile = image.GetExifProfile();
            var result = new Dictionary<string, string>();

            if (profile != null)
            {
                foreach (var value in profile.Values)
                {
                    string key = value.Tag.ToString();
                    string val;

                    if (value is IExifValue<byte[]> bytesVal && key.StartsWith("XP"))
                        val = Encoding.Unicode.GetString(bytesVal.Value).TrimEnd('\0');
                    else
                        val = value.ToString();

                    result[key] = val;
                }
            }

            return result;
        }

        private void CreateWidgets(string title, DataType type, string icon, List<string> values = null)
        {
            var layout = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5)
            };
            MetadataPanel.Children.Add(layout);

            layout.Children.Add(new Image
            {
                Width = 24,
                Height = 24,
                Source = new SvgImageSource(new Uri("ms-appx:///Assets/" + icon + ".svg"))
                {
                    RasterizePixelWidth = 24,
                    RasterizePixelHeight = 24
                },
                Margin = new Thickness(5, 2, 0, 0),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Right
            });

            var rightLayout = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(5, 2, 0, 0)
            };
            layout.Children.Add(rightLayout);

            if (type == DataType.MULTISTRING)
            {
                rightLayout.Children.Add(new TextBlock
                {
                    Text = title,
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Left
                });
            }

            if (type == DataType.STRING)
            {
                rightLayout.Children.Add(new TextBox
                {
                    PlaceholderText = title,
                    FontSize = 14,
                    Text = values[0],
                    HorizontalAlignment = HorizontalAlignment.Left,
                    TextWrapping = TextWrapping.Wrap,
                    Width = 290,
                    Height = 30
                });
            }
            else if (type == DataType.MULTILINESTRING)
            {
                rightLayout.Children.Add(new RichEditBox
                {
                    PlaceholderText = title,
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    TextWrapping = TextWrapping.Wrap,
                    Width = 290,
                    Height = 60
                });
            }
            else if (type == DataType.MULTISTRING)
            {
                var dataLayout = new StackPanel
                {
                    Orientation = Orientation.Vertical
                };

                int total = values.Count;
                int columns = 5;
                int rows = (total + columns - 1) / columns;
                int counter = 0;

                for (int row = 0; row < rows; row++)
                {
                    var rowLayout = new StackPanel
                    {
                        Orientation = Orientation.Horizontal
                    };

                    for (int col = 0; col < columns; col++)
                    {
                        int index = row * columns + col;
                        if (index >= total)
                            break;

                        var textBlock = new TextBlock
                        {
                            Text = values[counter],
                            FontSize = 13,
                            Margin = new Thickness(0, 0, 10, 0)
                        };
                        rowLayout.Children.Add(textBlock);

                        counter++;
                    }

                    dataLayout.Children.Add(rowLayout);
                }

                rightLayout.Children.Add(dataLayout);
            }
            else if (type == DataType.DATE)
            {
                DateTime? selectedDate = null;
                if (values[0] != null && DateTime.TryParse(values[0], out DateTime parsedDate))
                {
                    selectedDate = parsedDate;
                }

                rightLayout.Children.Add(new CalendarDatePicker
                {
                    PlaceholderText = title,
                    Width = 290,
                    FontSize = 14,
                    Date = selectedDate,
                    DateFormat = "{dayofweek.full}‎, ‎{month.full}‎ ‎{day.integer}‎, ‎{year.full}"
                });
            }
        }

        private async void OnCountClicked(object sender, RoutedEventArgs e)
        {
        }
    }
}
