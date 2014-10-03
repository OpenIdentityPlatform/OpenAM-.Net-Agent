using ru.org.openam.sdk;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using ru.org.openam.sdk.sts;
using System.IdentityModel.Tokens;
using ru.org.openam.sdk.auth.callback;

namespace ru.org.openam.sdk.junit
{
    
    
    /// <summary>
    ///This is a test class for STSTest and is intended
    ///to contain all STSTest Unit Tests
    ///</summary>
    [TestClass()]
    public class STSTest
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
        ///A test for getToken
        ///</summary>
        [TestMethod()]
        [DeploymentItem("ru.org.openam.sdk.dll")]
        public void getTokenTest()
        {
            //SecurityToken actual= STS.getToken(
            //    new sts.Token(
            //        Auth.login("/", auth.indexType.moduleInstance, "Application", new Callback[] { new NameCallback(Config.getAppUser()), new PasswordCallback(Config.getAppPassword())}),
            //        Auth.login("/clients", auth.indexType.service, "ldap", new Callback[] { new NameCallback("11111111111"), new PasswordCallback("1111111111") })
            //        )
            //    );
            //service=ldap&IDToken1=11111111111&IDToken2=1111111111
        }
    }
}
