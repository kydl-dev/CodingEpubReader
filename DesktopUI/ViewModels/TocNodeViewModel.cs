using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Application.DTOs;
using Avalonia;
using Avalonia.Media;

namespace DesktopUI.ViewModels;

public sealed class TocNodeViewModel
{
    public TocNodeViewModel(TocItemDto item)
    {
        Item = item;
        Children = new ObservableCollection<TocNodeViewModel>(
            item.Children.Select(child => new TocNodeViewModel(child)));
        IsExpanded = item.Depth <= 1;
    }

    public TocItemDto Item { get; }
    public string Id => Item.Id;
    public string Title => Item.Title;
    public string ContentSrc => Item.ContentSrc;
    public int Depth => Item.Depth;
    public ObservableCollection<TocNodeViewModel> Children { get; }
    public bool IsExpanded { get; set; }

    public bool HasContent => !string.IsNullOrWhiteSpace(ContentSrc);
    public bool IsSectionHeader => !HasContent;
    public bool IsSubchapter => HasContent && Depth > 1;
    public FontWeight TitleWeight => IsSubchapter ? FontWeight.Normal : FontWeight.SemiBold;
    public Thickness AdditionalIndent => IsSubchapter ? new Thickness(10, 0, 0, 0) : new Thickness(0);

    public IEnumerable<TocNodeViewModel> Flatten()
    {
        yield return this;
        for (var index = 0; index < Children.Count; index++)
        {
            var child = Children[index];
            foreach (var descendant in child.Flatten())
                yield return descendant;
        }
    }
}