using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.Android;
using Xamarin.UITest.Queries;

namespace AutoBackup.UITests
{
	[TestFixture]
	public class Tests
	{
		AndroidApp app;

		[SetUp]
		public void BeforeEachTest ()
		{
			// TODO: If the Android app being tested is included in the solution then open
			// the Unit Tests window, right click Test Apps, select Add App Project
			// and select the app projects that should be tested.
			app = ConfigureApp
				.Android
				.ApkFile ("../../../bin/Release/com.xamarin.autobackup-Signed.apk")
				.ApiKey ("024b0d715a7e9c22388450cf0069cb19")
				.StartApp ();
		}

		[Test]
		public void AutoBackup_VerifyMainScreen_ShouldDisplay ()
		{
			Assert.That(app.Query (c => c.Id ("action_bar_container")).Any());
			Assert.That(app.Query (c => c.Id ("action_add_file")).Any());
			Assert.That(app.Query (c => c.Text ("Automatic Backup")).Any());
			Assert.That(app.Query (c => c.Text ("File name")).Any());
			Assert.That(app.Query (c => c.Text ("Size (bytes)")).Any());
			if (app.Query (c => c.Class ("ListView")).Any ()) {
				Assert.That(app.Query (c => c.Id ("file_list")).Any());
				Assert.That(app.Query (c => c.Id ("file_name")).Any());
				Assert.That(app.Query (c => c.Id ("file_size")).Any());
			} else {
				Assert.That(app.Query (c => c.Id ("empty_file_list_message")).Any());
			}
		}

		[Test]
		public void AutoBackup_VerifyAddScreen_ShouldDisplay ()
		{
			app.WaitForElement (c => c.Id ("action_add_file"));
			app.Tap (c => c.Id ("action_add_file"));
			app.WaitForElement (c => c.Id ("action_bar_container"));
			Assert.That(app.Query (c => c.Id ("action_bar_container")).Any());
			Assert.That(app.Query (c => c.Text ("AddFileActivity")).Any());
			Assert.That(app.Query (c => c.Id ("file_label")).Any());
			Assert.That(app.Query (c => c.Id ("file_name")).Any());
			Assert.That(app.Query (c => c.Id ("size_label")).Any());
			Assert.That(app.Query (c => c.Id ("file_size")).Any());
			Assert.That(app.Query (c => c.Id ("file_size_spinner")).Any());
			Assert.That(app.Query (c => c.Id ("storage_label")).Any());
			Assert.That(app.Query (c => c.Id ("storage_spinner")).Any());
			Assert.That(app.Query (c => c.Id ("create_file_button")).Any());
		}

		[Test]
		public void AutoBackup_AddFileWithoutChanges_ShouldAdd ()
		{
			app.WaitForElement (c => c.Id ("action_add_file"));
			var originalFileListCount = app.Query (c => c.Id ("empty_file_list_message")).Any () ? 0 : Convert.ToInt32 (app.Query (c => c.Id ("file_list").Invoke ("getCount")).Cast<int> ().FirstOrDefault ());
			AddFile ();
			var newFileListCount = Convert.ToInt32 (app.Query (c => c.Id ("file_list").Invoke ("getCount")).FirstOrDefault ());
			Assert.Greater (newFileListCount, originalFileListCount);
		}

		[Test]
		public void AutoBackup_AddFileWithChanges_ShouldAdd ()
		{
			app.WaitForElement (c => c.Id ("action_add_file"));
			var options = new FileOptions () {
				FileName = "xamarin.md",
				Size = 50,
				SizeType = SizeType.Kilobytes,
				StorageType = StorageType.DonotBackup
			};
			AddFile (options);
			Assert.That (app.Query (c => c.Id ("file_name")).Any (c => c.Text.Contains (options.FileName)));
			Assert.That (app.Query (c => c.Id ("file_size")).Any (c => c.Text.Contains (string.Format("{0:n0}", options.ByteCount()))));

		}

		void AddFile(FileOptions options = null)
		{
			app.Tap (c => c.Id ("action_add_file"));
			app.WaitForElement (c => c.Id ("create_file_button"));
			if (options != null) {
				var fileNameTextField = app.Query (c => c.Id ("file_name")).FirstOrDefault ();
				if (options.FileName != fileNameTextField.Text) {
					app.Tap (c => c.Id ("file_name"));
					app.ClearText ();
					app.EnterText (options.FileName);
				}
				var fileSizeTextField = app.Query (c => c.Id ("file_size")).FirstOrDefault ();
				if (options.Size != Convert.ToInt32 (fileSizeTextField.Text)) {
					app.Tap (c => c.Id ("file_size"));
					app.ClearText ();
					app.EnterText (options.Size.ToString());
				}
				if (options.Size.ToString() != app.Query (c => c.Id ("file_size_spinner").Invoke("getSelectedItem")).FirstOrDefault ().ToString().Replace(" ", "")) {
					app.Query (c => c.Id ("file_size_spinner").Invoke ("setSelection", (int)options.SizeType)).FirstOrDefault ();
				}
				if (options.Size.ToString() != app.Query (c => c.Id ("storage_spinner").Invoke("getSelectedItem")).FirstOrDefault().ToString().Replace(" ", "")) {
					app.Query (c => c.Id ("storage_spinner").Invoke ("setSelection", (int)options.SizeType)).FirstOrDefault ();
				}
			}
			app.Tap (c => c.Id ("create_file_button"));
			app.WaitForElement (c => c.Id ("action_add_file"));
		}

		class FileOptions
		{
			public string FileName {get;set;} = "foo.txt";
			public int Size {get;set;} = 10;
			public SizeType SizeType {get;set;} = SizeType.Bytes;
			public StorageType StorageType {get;set;} = StorageType.Internal;
			public int ByteCount() 
			{
				switch (SizeType) {
				case SizeType.Kilobytes:
					return Size * 1024;
				case SizeType.Megabytes:
					return Size * 1024 * 1024;
				default:
					return Size;
				}
			}
		}

		enum SizeType
		{
			Bytes,
			Kilobytes,
			Megabytes
		}

		enum StorageType
		{
			Internal,
			External,
			DonotBackup
		}
	}
}