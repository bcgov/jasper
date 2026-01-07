namespace Scv.Core.Helpers.Exceptions
{
    /// <summary>
    /// NotFoundException class, provides a way to handle not found exceptions so that they are returned by the middleware in a standardized way.
    /// </summary>
    public class NotFoundException : Exception
    {
        #region Constructors

        /// <summary>
        /// Creates a new instance of a NotFoundException object, initializes it with the specified arguments.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public NotFoundException(string message) : base(message) { }

        /// <summary>
        /// Creates a new instance of a NotFoundException object, initializes it with the specified arguments.
        /// /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        /// <returns></returns>
        public NotFoundException(string message, Exception innerException) : base(message, innerException) { }

        #endregion Constructors
    }
}