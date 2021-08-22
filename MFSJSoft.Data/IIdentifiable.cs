
namespace MFSJSoft.Data
{

    /// <summary>
    /// Represents an object that identifies itself through the <see cref="Id"/> property. Objects
    /// that use impelentors of this interface should explain its use in context.
    /// </summary>
    public interface IIdentifiable
    {

        /// <summary>
        /// A value idnetifying the implementing object within the context it is intended to be used.
        /// </summary>
        object Id { get; }

    }
}
