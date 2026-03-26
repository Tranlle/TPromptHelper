using System.Collections.Generic;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace TPromptHelper.Desktop.Controls;

/// <summary>
/// 支持 Markdown 渲染的 TextBlock 控件
/// </summary>
public class MarkdownTextBlock : UserControl
{
    private static readonly Regex InlineCodeRegex = new(@"`([^`]+)`", RegexOptions.Compiled);
    private static readonly Regex BoldRegex = new(@"\*\*([^*]+)\*\*", RegexOptions.Compiled);
    private static readonly Regex ItalicRegex = new(@"\*([^*]+)\*", RegexOptions.Compiled);

    /// <summary>
    /// Markdown 文本依赖属性
    /// </summary>
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MarkdownTextBlock, string>(nameof(Text), string.Empty);

    /// <summary>
    /// Markdown 源代码
    /// </summary>
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public MarkdownTextBlock()
    {
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == TextProperty)
        {
            UpdateText();
        }
    }

    private void UpdateText()
    {
        var text = Text ?? string.Empty;
        var panel = new StackPanel { Spacing = 6 };

        try
        {
            var lines = text.Split('\n');
            var inCodeBlock = false;
            var codeBlockLines = new List<string>();
            var codeBlockFence = string.Empty;

            foreach (var line in lines)
            {
                // 代码块处理
                if (line.StartsWith("```"))
                {
                    if (!inCodeBlock)
                    {
                        inCodeBlock = true;
                        codeBlockFence = line.TrimStart('`');
                        codeBlockLines.Clear();
                    }
                    else
                    {
                        // 代码块结束，添加代码块
                        panel.Children.Add(CreateCodeBlock(codeBlockFence, string.Join("\n", codeBlockLines)));
                        inCodeBlock = false;
                        codeBlockLines.Clear();
                    }
                    continue;
                }

                if (inCodeBlock)
                {
                    codeBlockLines.Add(line);
                    continue;
                }

                // 处理标题
                if (line.StartsWith("### "))
                    panel.Children.Add(CreateHeading(line.TrimStart('#', ' '), 3));
                else if (line.StartsWith("## "))
                    panel.Children.Add(CreateHeading(line.TrimStart('#', ' '), 2));
                else if (line.StartsWith("# "))
                    panel.Children.Add(CreateHeading(line.TrimStart('#', ' '), 1));
                // 处理无序列表
                else if (line.TrimStart().StartsWith("- ") || line.TrimStart().StartsWith("* "))
                    panel.Children.Add(CreateBullet(line.Trim()));
                // 处理有序列表
                else if (line.TrimStart().Length > 0 && char.IsDigit(line.TrimStart()[0]))
                    panel.Children.Add(CreateOrderedList(line.Trim()));
                // 处理分隔线
                else if (line.Trim() == "---" || line.Trim() == "***" || line.Trim() == "___")
                    panel.Children.Add(CreateHorizontalRule());
                // 处理引用块
                else if (line.StartsWith("> "))
                    panel.Children.Add(CreateBlockQuote(line.TrimStart(' ', '>')));
                // 普通文本（支持内联格式）
                else if (!string.IsNullOrWhiteSpace(line))
                    panel.Children.Add(CreateParagraph(line));
            }

            // 处理未关闭的代码块
            if (inCodeBlock && codeBlockLines.Count > 0)
            {
                panel.Children.Add(CreateCodeBlock(codeBlockFence, string.Join("\n", codeBlockLines)));
            }
        }
        catch
        {
            // 解析失败，回退到纯文本
            panel.Children.Clear();
            panel.Children.Add(new TextBlock
            {
                Text = text,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            });
        }

        Content = new ScrollViewer
        {
            Content = panel,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };
    }

    private TextBlock CreateHeading(string text, int level)
    {
        var fontSize = level switch
        {
            1 => 24,
            2 => 20,
            3 => 16,
            _ => 14
        };

        var fontWeight = level <= 2 ? FontWeight.SemiBold : FontWeight.Bold;
        var margin = new Thickness(0, level == 1 ? 8 : 4, 0, 4);

        return new TextBlock
        {
            Text = ParseInlineMarkdown(text),
            FontSize = fontSize,
            FontWeight = fontWeight,
            Margin = margin,
            TextWrapping = TextWrapping.Wrap
        };
    }

    private TextBlock CreateParagraph(string text)
    {
        return new TextBlock
        {
            Text = ParseInlineMarkdown(text),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 4)
        };
    }

    private StackPanel CreateBullet(string text)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text = "•",
            Margin = new Thickness(12, 0, 0, 0)
        });
        panel.Children.Add(new TextBlock
        {
            Text = ParseInlineMarkdown(text.TrimStart('-', '*', ' ')),
            TextWrapping = TextWrapping.Wrap
        });
        return panel;
    }

    private StackPanel CreateOrderedList(string text)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var trimmed = text.TrimStart();
        var number = string.Empty;
        var i = 0;
        while (i < trimmed.Length && char.IsDigit(trimmed[i]))
        {
            number += trimmed[i];
            i++;
        }
        panel.Children.Add(new TextBlock
        {
            Text = number + ".",
            Margin = new Thickness(12, 0, 0, 0),
            MinWidth = 24
        });
        panel.Children.Add(new TextBlock
        {
            Text = ParseInlineMarkdown(trimmed.TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.', ' ')),
            TextWrapping = TextWrapping.Wrap
        });
        return panel;
    }

    private Border CreateCodeBlock(string fence, string code)
    {
        var isInline = string.IsNullOrEmpty(fence);
        var lang = isInline ? "" : fence.Trim();

        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#1E1E1E")),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12, 8),
            Margin = new Thickness(0, 4),
            Child = new TextBlock
            {
                Text = code,
                FontFamily = new FontFamily("Cascadia Code, Consolas, monospace"),
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.Parse("#D4D4D4")),
                TextWrapping = TextWrapping.Wrap
            }
        };
    }

    private Border CreateBlockQuote(string text)
    {
        return new Border
        {
            BorderBrush = new SolidColorBrush(Color.Parse("#4EC994")),
            BorderThickness = new Thickness(3, 0, 0, 0),
            Padding = new Thickness(12, 4),
            Margin = new Thickness(0, 4),
            Background = new SolidColorBrush(Color.Parse("#252526")),
            Child = new TextBlock
            {
                Text = ParseInlineMarkdown(text),
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.Parse("#9CDCFE"))
            }
        };
    }

    private Border CreateHorizontalRule()
    {
        return new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Color.Parse("#333333")),
            Margin = new Thickness(0, 8),
            BorderThickness = new Thickness(0)
        };
    }

    /// <summary>
    /// 解析内联 Markdown 格式（粗体、斜体、行内代码）
    /// </summary>
    private string ParseInlineMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // 移除块级格式标记，保留文本
        // 行内代码 `code`
        text = InlineCodeRegex.Replace(text, "$1");

        // 粗体 **text** 或 __text__
        text = BoldRegex.Replace(text, "$1");

        // 斜体 *text* 或 _text_
        text = ItalicRegex.Replace(text, "$1");

        return text;
    }
}
