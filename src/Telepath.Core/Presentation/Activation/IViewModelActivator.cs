namespace Telepath.Core;

/// <summary>
/// Creates page ViewModels without taking ownership. The caller must pass the
/// instance to <see cref="INavigator.Navigate"/> or <see cref="IOverlay.Push"/>.
/// </summary>
public interface IViewModelActivator
{
    T Create<T>(params object[] arguments) where T : class, IViewModel;
}
