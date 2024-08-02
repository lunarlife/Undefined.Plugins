namespace Undefined.Plugins;

public class PluginsSpace<TBase>
{
    private readonly Type _pluginBase;

    public PluginsSpace(Type pluginBase, Type mainPlugin)
    {
        _pluginBase = pluginBase;
    }

    public static PluginsSpace<TBase> Create<TMain>() => new(typeof(TBase), typeof(TMain));
}