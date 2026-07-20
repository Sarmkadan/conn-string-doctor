using System;
using System.Collections.Generic;

namespace ConnStringDoctor
{
    /// <summary>
    /// Tests for ConnectionStringRedactor.
    /// </summary>
    public static class ConnectionStringRedactorTests
    {
        /// <summary>
        /// Runs tests for RedactToDictionary.
        /// </summary>
        public static void RunTests()
        {
            var connectionString = "Server=myServer;Password=myPassword;User Id=myUser;";
            var result = ConnectionStringRedactor.RedactToDictionary(connectionString);
            
            if (result["Password"] != "***") throw new Exception("Password not masked");
            if (result["Server"] != "myServer") throw new Exception("Server not preserved");
            if (result["User Id"] != "***") throw new Exception("User Id not masked");
            
            Console.WriteLine("RedactToDictionary tests passed");
        }
    }
}
