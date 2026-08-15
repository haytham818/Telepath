using Telepath.Core;

namespace Telepath.Godot;

/// <summary>
/// Host-assembly Telepath view contract. Generated views implement this so a parent
/// can inject an item ViewModel before the child enters the tree.
/// </summary>
public interface ITelepathView<TViewModel>
    where TViewModel : class, IViewModel
{
    TViewModel? ViewModel { get; set; }
}
