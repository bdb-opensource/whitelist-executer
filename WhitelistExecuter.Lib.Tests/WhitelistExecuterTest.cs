using WhitelistExecuter.Lib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Configuration;
using System.Diagnostics;

namespace WhitelistExecuter.Lib.Tests
{


    [TestClass()]
    public class WhitelistExecuterTest
    {


        private TestContext testContextInstance;

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


        [TestMethod()]
        public void ExecuteCommandTest()
        {
            var baseDir = @"C:\Temp\Dir1";
            Directory.CreateDirectory(baseDir);
            Directory.CreateDirectory(@"C:\Temp\Dir2");

            var target = new WhitelistExecuter();
            var relativePath = Path.GetRandomFileName();
            var targetPath = Path.Combine(baseDir, relativePath);
            Directory.CreateDirectory(targetPath);
            var scriptFile = ConfigurationManager.AppSettings["ScriptFilePath"];
            File.WriteAllText(Path.Combine(targetPath, scriptFile), "@echo OK\n");
            var actual = target.ExecuteCommand(baseDir, Command.RUN_SCRIPT, relativePath);
            Debug.WriteLine("stdout: " + actual.StandardOutput);
            Debug.WriteLine("stderr: " + actual.StandardError);
            Assert.AreEqual(0, actual.ExitCode);
            Assert.AreEqual("OK", actual.StandardOutput.Trim());
        }
    }
}
