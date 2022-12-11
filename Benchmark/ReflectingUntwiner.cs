//Author MikeNakis (michael.gr)
namespace MikeNakis.Intertwine
{
    /// <summary>
    /// An untwiner which uses reflection to do its job. Keep for reference. WARNING: SLOW AS MOLASSES.
    /// </summary>
    public class ReflectingUntwiner: Untwiner
    {
        private readonly object _Target;
        private readonly System.Reflection.MethodInfo[] Methodinfos;

        public ReflectingUntwiner( System.Type twinee, object target ) 
            :base( twinee )
        { 
            _Target = target;
            Methodinfos = Twinee.GetMethods( System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance );
        }

        public override object Target { get { return _Target; } }

        public override object AnyCall( int selector, object[] arguments )
        {
            return Methodinfos[selector].Invoke( Target, arguments );
        }
    }
}
