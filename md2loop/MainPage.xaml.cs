using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace md2loop;

public sealed partial class MainPage : Page
{
    private ClipboardMode _currentMode = ClipboardMode.Unknown;
    private string? _clipboardText;
    private string? _clipboardHtml;
    private string? _clipboardRtf;
    private DispatcherTimer? _feedbackTimer;
    private bool _isReading;
    private bool _isSubscribed;

    public MainPage()
    {
        InitializeComponent();

        var accelerator = new KeyboardAccelerator
        {
            Key = Windows.System.VirtualKey.Enter,
            Modifiers = Windows.System.VirtualKeyModifiers.Control
        };
        accelerator.Invoked += (_, args) =>
        {
            ConvertRecommended();
            args.Handled = true;
        };
        KeyboardAccelerators.Add(accelerator);
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_isSubscribed)
        {
            Clipboard.ContentChanged += OnClipboardContentChanged;
            _isSubscribed = true;
        }

        _ = RefreshAsync();
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_isSubscribed)
        {
            Clipboard.ContentChanged -= OnClipboardContentChanged;
            _isSubscribed = false;
        }

        _feedbackTimer?.Stop();
        _feedbackTimer = null;
    }

    /// <summary>
    /// Re-reads the clipboard when the window is activated. ContentChanged is not
    /// always delivered to a desktop app that is not in the foreground, so this
    /// covers changes made while another app had focus.
    /// </summary>
    public void OnWindowActivated() => _ = RefreshAsync();

    private void OnClipboardContentChanged(object? sender, object e)
    {
        // The event does not necessarily arrive on the UI thread.
        DispatcherQueue.TryEnqueue(() => _ = RefreshAsync());
    }

    private async Task RefreshAsync()
    {
        if (_isReading)
            return;

        _isReading = true;
        try
        {
            var snapshot = await ClipboardManager.ReadAsync();

            if (snapshot.IsExcluded)
            {
                ShowClipboardExcluded();
                return;
            }

            var text = snapshot.Text;
            var html = snapshot.Html;
            var rtf = snapshot.Rtf;

            _clipboardText = text;
            _clipboardHtml = html;
            _clipboardRtf = rtf;

            var length = text?.Length ?? html?.Length ?? rtf?.Length ?? 0;
            var mode = ClipboardContentDetector.Detect(text, html, rtf);
            _currentMode = mode;

            ClipboardLengthText.Text = length > 0 ? $"{length} characters" : "Empty";
            RichTextButton.IsEnabled = !string.IsNullOrWhiteSpace(text);
            MarkdownButton.IsEnabled =
                !string.IsNullOrWhiteSpace(html) || ClipboardContentDetector.ContainsRTF(rtf);

            switch (mode)
            {
                case ClipboardMode.Markdown:
                    ModeIcon.Glyph = "\uE73E"; // Checkmark
                    ModeIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
                    ModeText.Text = "Detected Markdown / plain text";
                    ShortcutText.Text = "Ctrl+Enter converts to Rich Text";
                    break;
                case ClipboardMode.RichText:
                    ModeIcon.Glyph = "\uE73E";
                    ModeIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);
                    ModeText.Text = "Detected Rich Text";
                    ShortcutText.Text = "Ctrl+Enter converts to Markdown";
                    break;
                default:
                    ModeIcon.Glyph = "\uE9CE"; // Question mark
                    ModeIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
                    ModeText.Text = "No convertible content";
                    ShortcutText.Text = "Copy text or rich text to begin";
                    break;
            }
        }
        catch (COMException)
        {
            ShowClipboardUnavailable();
        }
        catch (UnauthorizedAccessException)
        {
            ShowClipboardUnavailable();
        }
        finally
        {
            _isReading = false;
        }
    }

    private void ShowClipboardUnavailable()
    {
        ResetClipboardState();
        ClipboardLengthText.Text = "Unavailable";
        ModeIcon.Glyph = "\uE7BA";
        ModeIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
        ModeText.Text = "Clipboard is temporarily busy";
        ShortcutText.Text = "Copy again or reactivate the window";
        RichTextButton.IsEnabled = false;
        MarkdownButton.IsEnabled = false;
    }

    private void ShowClipboardExcluded()
    {
        ResetClipboardState();
        ClipboardLengthText.Text = "Private";
        ModeIcon.Glyph = "\uE72E"; // Lock
        ModeIcon.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray);
        ModeText.Text = "Content is marked private";
        ShortcutText.Text = "This content was not read";
        RichTextButton.IsEnabled = false;
        MarkdownButton.IsEnabled = false;
    }

    private void ResetClipboardState()
    {
        _currentMode = ClipboardMode.Unknown;
        _clipboardText = null;
        _clipboardHtml = null;
        _clipboardRtf = null;
    }

    private void RichTextButton_Click(object sender, RoutedEventArgs e) => ConvertToRichText();

    private void MarkdownButton_Click(object sender, RoutedEventArgs e) => ConvertToMarkdown();

    private void ConvertRecommended()
    {
        if (_currentMode == ClipboardMode.Markdown)
            ConvertToRichText();
        else if (_currentMode == ClipboardMode.RichText)
            ConvertToMarkdown();
        else
            ShowFeedback("Clipboard has no convertible content", success: false);
    }

    private void ConvertToRichText()
    {
        if (string.IsNullOrWhiteSpace(_clipboardText))
        {
            ShowFeedback("Clipboard has no text to convert", success: false);
            return;
        }

        var html = LoopHtmlConverter.Convert(_clipboardText);
        try
        {
            ClipboardManager.WriteForLoop(html, _clipboardText);
            ShowFeedback("Rich text copied - ready for Loop", success: true);
        }
        catch (COMException)
        {
            ShowFeedback("Clipboard is busy. Try again.", success: false);
        }
    }

    private void ConvertToMarkdown()
    {
        string? markdown = null;

        if (!string.IsNullOrWhiteSpace(_clipboardHtml))
        {
            markdown = HtmlToMarkdownConverter.Convert(_clipboardHtml);
        }

        if (string.IsNullOrWhiteSpace(markdown) &&
            RtfToMarkdownConverter.TryConvert(_clipboardRtf, out var rtfMarkdown))
        {
            markdown = rtfMarkdown;
        }

        if (string.IsNullOrWhiteSpace(markdown))
        {
            ShowFeedback("Could not read rich text", success: false);
            return;
        }

        try
        {
            ClipboardManager.WriteMarkdown(markdown);
            ShowFeedback("Markdown copied", success: true);
        }
        catch (COMException)
        {
            ShowFeedback("Clipboard is busy. Try again.", success: false);
        }
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

        _feedbackTimer?.Stop();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _feedbackTimer = timer;
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            if (!ReferenceEquals(_feedbackTimer, timer))
                return;

            FeedbackPanel.Visibility = Visibility.Collapsed;
            _feedbackTimer = null;
        };
        timer.Start();
    }
}
