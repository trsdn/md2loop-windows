using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;

namespace md2loop;

public sealed partial class MainPage : Page
{
    private ClipboardMode _currentMode = ClipboardMode.Unknown;
    private string? _clipboardText;
    private string? _clipboardHtml;
    private DispatcherTimer? _timer;

    public MainPage()
    {
        InitializeComponent();

        KeyboardAccelerators.Add(new KeyboardAccelerator
        {
            Key = Windows.System.VirtualKey.Enter,
            Modifiers = Windows.System.VirtualKeyModifiers.Control
        });
        KeyboardAccelerators[0].Invoked += (_, _) => Convert();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += async (_, _) => await PollClipboardAsync();
        _timer.Start();

        _ = PollClipboardAsync();
    }

    private async Task PollClipboardAsync()
    {
        try
        {
            var (text, html) = await ClipboardManager.ReadAsync();
            _clipboardText = text;
            _clipboardHtml = html;

            var length = text?.Length ?? html?.Length ?? 0;
            var mode = ClipboardContentDetector.Detect(text, html);
            _currentMode = mode;

            ClipboardLengthText.Text = length > 0 ? $"{length} characters" : "Empty";

            switch (mode)
            {
                case ClipboardMode.Markdown:
                    ModeIcon.Glyph = "\uE73E"; // Checkmark
                    ModeIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
                    ModeText.Text = "Markdown";
                    ConvertText.Text = "Convert to Loop";
                    ConvertIcon.Glyph = "\uE72A"; // Arrow right
                    ConvertButton.IsEnabled = true;
                    break;
                case ClipboardMode.RichText:
                    ModeIcon.Glyph = "\uE73E";
                    ModeIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);
                    ModeText.Text = "Rich Text";
                    ConvertText.Text = "Convert to Markdown";
                    ConvertIcon.Glyph = "\uE72B"; // Arrow left
                    ConvertButton.IsEnabled = true;
                    break;
                default:
                    ModeIcon.Glyph = "\uE9CE"; // Question mark
                    ModeIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
                    ModeText.Text = "Unrecognized format";
                    ConvertText.Text = "Convert";
                    ConvertIcon.Glyph = "\uE8AB";
                    ConvertButton.IsEnabled = false;
                    break;
            }
        }
        catch
        {
            // Clipboard access can fail if another app holds it
        }
    }

    private void ConvertButton_Click(object sender, RoutedEventArgs e) => Convert();

    private void Convert()
    {
        if (_currentMode == ClipboardMode.Markdown)
            ConvertToLoop();
        else if (_currentMode == ClipboardMode.RichText)
            ConvertToMarkdown();
    }

    private void ConvertToLoop()
    {
        if (string.IsNullOrWhiteSpace(_clipboardText)) return;

        var html = LoopHtmlConverter.Convert(_clipboardText);
        ClipboardManager.WriteForLoop(html, _clipboardText);
        ShowFeedback("Ready to paste into Loop", success: true);
    }

    private void ConvertToMarkdown()
    {
        string? markdown = null;

        if (!string.IsNullOrWhiteSpace(_clipboardHtml))
        {
            markdown = HtmlToMarkdownConverter.Convert(_clipboardHtml);
        }

        if (string.IsNullOrWhiteSpace(markdown))
        {
            ShowFeedback("Could not read rich text", success: false);
            return;
        }

        ClipboardManager.WriteMarkdown(markdown);
        ShowFeedback("Markdown copied", success: true);
    }

    private void ShowFeedback(string message, bool success)
    {
        FeedbackPanel.Visibility = Visibility.Visible;
        FeedbackText.Text = message;
        FeedbackIcon.Glyph = success ? "\uE73E" : "\uE7BA";
        var color = success ? Microsoft.UI.Colors.Green : Microsoft.UI.Colors.Red;
        var brush = new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
        FeedbackIcon.Foreground = brush;
        FeedbackText.Foreground = brush;

        var hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        hideTimer.Tick += (_, _) =>
        {
            FeedbackPanel.Visibility = Visibility.Collapsed;
            hideTimer.Stop();
        };
        hideTimer.Start();
    }
}

