using CommunityToolkit.Mvvm.ComponentModel;
using TongaKids.Models;

namespace TongaKids.ViewModels;

public sealed partial class ChoiceCardModel(PhonicsItem item) : ObservableObject
{
    public PhonicsItem Item { get; } = item;

    public string Grapheme => Item.Grapheme;

    [ObservableProperty] private bool _isSelected;
}
