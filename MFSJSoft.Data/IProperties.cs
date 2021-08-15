
namespace MFSJSoft.Data
{
	/// <summary>
	/// Represents a generic read-only accessor to string-valued properties.
	/// </summary>
    public interface IProperties
    {
		
		/// <summary>
		/// Returns the value of the property matching the given <c>name</c>. If not found,
		/// return <see langword="null" />
		/// </summary>
        string GetProperty(string name);
    }
}
