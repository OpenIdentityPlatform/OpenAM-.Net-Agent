using ru.org.openam.sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using ru.org.openam.sdk.session;
using ru.org.openam.sdk.auth.callback;

namespace ru.org.openam.sdk.junit
{
    
    
    /// <summary>
    ///This is a test class for SessionTest and is intended
    ///to contain all SessionTest Unit Tests
    ///</summary>
    [TestClass()]
    public class PlicyTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for Get
        ///</summary>
        [TestMethod()]
        public void policy_GetTest()
        {
            Policy.Get(
                new Agent(),
                Auth.login("/clients", auth.indexType.service, "ldap", new Callback[] { new NameCallback("11111111111"), new PasswordCallback("1111111111") }),
                new Uri("http://sssss:80/sdsd?sdsdsd"),
                null
            );
        }

        //[TestMethod()]
        //public void session_bad_GetTest()
        //{
        //    try
        //    {
        //        Session Session = new Session("000000000000000000000000000000");
        //    }
        //    catch (SessionException)
        //    {
        //        return;
        //    }
        //}
    }
}
