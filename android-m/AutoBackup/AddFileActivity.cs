
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Text;
using Android.Util;
using Java.IO;

namespace AutoBackup
{
	/**
 	* The purpose of AddFileActivity activity is to create a data file based on the
 	* file name and size parameters specified as an Intent external parameters or with the
 	* activity UI.
 	* 
 	* The activity will return an MainActivityFragment#ADD_FILE_RESULT_ERROR
 	* if intent parameters are specified incorrectly or it will display Toast messages to the user
 	* if those parameters are specified via the activity UI.
 	*/
	[Activity (Label = "AddFileActivity", Theme = "@style/AppTheme")]			
	public class AddFileActivity : Activity
	{
		static readonly string TAG = "AutoBackupSample";

		/**
     	* The intent parameter that specifies a file name. The file name must be unique for the
     	* application internal directory.
     	*/
		public static readonly string FILE_NAME = "file_name";

		/**
     	* The intent parameter that specifies a file size in bytes. The size must be a number
     	* larger or equal to 0.
     	*/
		public static readonly string FILE_SIZE_IN_BYTES = "file_size_in_bytes";

		/**
     	* The file storage is an optional parameter. It should be one of these:
     	* "INTERNAL", "EXTERNAL", "DONOTBACKUP". The default option is "INTERNAL".
     	*/
		public static readonly string FILE_STORAGE = "file_storage";

		/**
     	* A file size multiplier. It is used to calculate the total number of bytes to be added
     	* to the file.
     	*/
		int sizeMultiplier = 1;

		/**
     	* Defines File Storage options.
     	*/
		enum FileStorage {
			Internal,
			External,
			DoNotBackup
		}

		/**
     	* Contains a selected by a user file storage option.
     	*/
		FileStorage fileStorage = FileStorage.Internal;
			
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			SetContentView (Resource.Layout.add_file);

			InitFileSizeSpinner ();
			InitFileStorageSpinner ();
		}

		protected override void OnResume ()
		{
			base.OnResume ();

			// If an intent has extra parameters, create the file and finish the activity.
			if (Intent.HasExtra (FILE_NAME) && Intent.HasExtra (FILE_SIZE_IN_BYTES)) {
				string fileName = Intent.GetStringExtra (FILE_NAME);
				string sizeInBytesParamValue = Intent.GetStringExtra (FILE_SIZE_IN_BYTES);
				string fileStorageParamValue = FileStorage.Internal.ToString ();

				if (Intent.HasExtra (FILE_STORAGE))
					fileStorageParamValue = Intent.GetStringExtra (FILE_STORAGE);

				if (TextUtils.IsEmpty (fileName) ||
						DoesFileExist (fileName) ||
						!IsSizeValid (sizeInBytesParamValue) ||
						!IsFileStorageParamValid (fileStorageParamValue)) {
					SetResult (Result.Canceled);
					Finish ();
					return;
				}

				fileStorage = (FileStorage)Enum.Parse (typeof (FileStorage), fileStorageParamValue);

				if (fileStorage == FileStorage.External && !Utils.IsExternalStorageAvailable ()) {
					SetResult (Result.Canceled);
					Finish ();
					return;
				}

				CreateFileWithRandomDataAndFinishActivity (fileName, fileStorage, sizeInBytesParamValue);
			}
		}

		/**
     	* A handler function for a Create File button click event.
     	*
     	* @param view a reference to the Create File button view.
     	*/
		public void OnCreateFileButtonClick (View view)
		{
			var fileNameEditText = FindViewById<EditText> (Resource.Id.file_name);
			var fileSizeEditText = FindViewById<EditText> (Resource.Id.file_size);
			string fileName = fileNameEditText.Text;
			string fileSizeEditTextValue = fileSizeEditText.Text;

			if (TextUtils.IsEmpty (fileName) || DoesFileExist (fileName)) {
				DisplayShortCenteredToast (GetString (Resource.String.file_exists));
				return;
			}

			if (!IsSizeValid (fileSizeEditTextValue)) {
				DisplayShortCenteredToast (GetString (Resource.String.file_size_is_invalid));
				return;
			}

			long fileSize = Convert.ToInt32 (fileSizeEditTextValue) * sizeMultiplier;

			if (fileStorage == FileStorage.External && !Utils.IsExternalStorageAvailable ()) {
				DisplayShortCenteredToast (GetString (Resource.String.external_storage_unavailable));
				return;
			}

			CreateFileWithRandomDataAndFinishActivity (fileName, fileStorage, fileSize.ToString ());
		}

	
		void InitFileSizeSpinner ()
		{
			var spinner = FindViewById<Spinner> (Resource.Id.file_size_spinner);
			var adapter = ArrayAdapter.CreateFromResource (this, Resource.Array.file_size_array,
				Android.Resource.Layout.SimpleSpinnerItem);
			adapter.SetDropDownViewResource (Android.Resource.Layout.SimpleSpinnerDropDownItem);
			spinner.Adapter = adapter;

			spinner.ItemSelected += delegate (object sender, AdapterView.ItemSelectedEventArgs e) {
				string sizeMeasure = adapter.GetItem (e.Position).ToString ();
				sizeMultiplier = (int)Math.Pow (1024, e.Position);

				if (Log.IsLoggable (TAG, LogPriority.Debug))
					Log.Debug (TAG, string.Format ("Selected: {0}, {1}", sizeMeasure, sizeMultiplier));
			};
		}

		private void InitFileStorageSpinner ()
		{
			var spinner = FindViewById<Spinner> (Resource.Id.storage_spinner);
			var adapter = ArrayAdapter.CreateFromResource (this, Resource.Array.file_storage_array,
				Android.Resource.Layout.SimpleSpinnerItem);
			adapter.SetDropDownViewResource (Android.Resource.Layout.SimpleSpinnerDropDownItem);
			spinner.Adapter = adapter;

			spinner.ItemSelected += delegate (object sender, AdapterView.ItemSelectedEventArgs e) {
				fileStorage = (FileStorage) e.Position;
			};
		}

		private void CreateFileWithRandomDataAndFinishActivity (string fileName, FileStorage storage, string sizeInBytes)
		{
			long size = Convert.ToInt32 (sizeInBytes);
			File file = null;
			System.IO.Stream fileOut = null;
			BufferedOutputStream bufOut = null;

			try {
				switch (storage) {
				case FileStorage.Internal:
					file = GetInternalFile (fileName);
					fileOut = OpenFileOutput (file.Name, FileCreationMode.Private);
					break;

				case FileStorage.External:
					if (!Utils.IsExternalStorageAvailable ()) {
						DisplayShortCenteredToast ("The external storage is not available");
					} else {
						File externalAppDir = GetExternalFilesDir (null);
						file = new File (externalAppDir, fileName);
						fileOut = new System.IO.FileStream (file.AbsolutePath, System.IO.FileMode.OpenOrCreate);
					}
					break;
				case FileStorage.DoNotBackup:
					file = new File (NoBackupFilesDir, fileName);
					fileOut = new System.IO.FileStream (file.AbsolutePath, System.IO.FileMode.OpenOrCreate);
					break;
				}

				if (file == null || fileOut == null) {
					Log.Debug (TAG, "Unable to create file output stream");
					// Returning back to the caller activity.
					SetResult (Result.Canceled);
					Finish ();
					return;
				}

				bufOut = new BufferedOutputStream (fileOut);
				for (int i = 0; i < size; i++) {
					var random = new Random ();
					var b = (byte) (255 * random.NextDouble ());
					bufOut.Write (b);
				}

				string message = string.Format ("File created: {0}, size: {1} bytes",
					file.AbsolutePath, sizeInBytes);

				DisplayShortCenteredToast (message);
				Log.Debug (TAG, message);

				// Returning back to the caller activity.
				SetResult (Result.Ok);
				Finish ();
			} catch (Exception e) {
				Log.Error (TAG, e.Message, e);
				// Returning back to the caller activity.
				SetResult (Result.Canceled);
				Finish ();
			} finally {
				if (bufOut != null) {
					try {
						bufOut.Close ();
					} catch (Exception) {
						// Ignore.
					}
				}
			}
		}

		void DisplayShortCenteredToast (string message)
		{
			var toast = Toast.MakeText(this, message, ToastLength.Short);
			toast.SetGravity (GravityFlags.CenterVertical, 0, 0);
			toast.Show ();
		}

		bool DoesFileExist (string fileName)
		{
			File file = GetInternalFile (fileName);
			if (file.Exists ()) {
				if (Log.IsLoggable (TAG, LogPriority.Debug))
					Log.Debug (TAG, "This file exists: " + file.Name);
				
				return true;
			}
			return false;
		}

		bool IsSizeValid (string sizeInBytesParamValue)
		{
			long sizeInBytes = 0;
			try {
				sizeInBytes = Convert.ToInt32 (sizeInBytesParamValue);
			} catch (Exception e) {
				if (Log.IsLoggable (TAG, LogPriority.Debug))
					Log.Debug (TAG, string.Format ("Invalid file size: {0}. {1}", sizeInBytesParamValue, e.Message));
				
				return false;
			}

			// Validate file size value. It should be 0 or a positive number.
			if (sizeInBytes < 0) {
				if (Log.IsLoggable (TAG, LogPriority.Debug))
					Log.Debug (TAG, "Invalid file size: " + sizeInBytes);
				
				return false;
			}
			return true;
		}

		bool IsFileStorageParamValid (string fileStorage)
		{
			if (Enum.IsDefined (typeof(FileStorage), fileStorage)) {
				return true;
			} else {
				if (Log.IsLoggable (TAG, LogPriority.Debug))
					Log.Debug (TAG, "Invalid file storage: " + fileStorage);
				
				return false;
			}
		}

		File GetInternalFile (string fileName)
		{
			return new File (FilesDir, fileName);
		}
						
	}
}

