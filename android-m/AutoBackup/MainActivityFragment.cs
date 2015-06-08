
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

using Java.IO;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;


namespace AutoBackup
{
	public class MainActivityFragment : Fragment
	{
		public static readonly int ADD_FILE_REQUEST = 1;

		ArrayAdapter<File> filesArrayAdapter;
		List<File> files;

		public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			SetHasOptionsMenu (true);
			return inflater.Inflate (Resource.Layout.fragment_main, container, false);
		}

		public override void OnActivityResult (int requestCode, Result resultCode, Intent data)
		{
			base.OnActivityResult (requestCode, resultCode, data);

			if (requestCode == ADD_FILE_REQUEST && resultCode == Result.Ok)
				UpdateListOfFiles ();
		}

		public override void OnCreateOptionsMenu (IMenu menu, MenuInflater inflater)
		{
			// Inflate the menu; this adds items to the action bar if it is present.
			inflater.Inflate (Resource.Menu.menu_main, menu);
			base.OnCreateOptionsMenu (menu, inflater);
		}

		public override bool OnOptionsItemSelected (IMenuItem item)
		{
			// Handle action bar item clicks here. The action bar will
			// automatically handle clicks on the Home/Up button, so long
			// as you specify a parent activity in AndroidManifest.xml.
			int id = item.ItemId;

			if (id == Resource.Id.action_settings) {
				return true;
			} else if (id == Resource.Id.action_add_file) {
				var addFileIntent = new Intent (Activity, typeof (AddFileActivity));
				StartActivityForResult (addFileIntent, ADD_FILE_REQUEST);
				return true;
			}

			return base.OnOptionsItemSelected (item);
		}

		public override void OnResume ()
		{
			base.OnResume ();
			if (filesArrayAdapter == null) {
				files = CreateListOfFiles ();
				filesArrayAdapter = new FileArrayAdapter (Activity, Resource.Layout.file_list_item, files);

				UpdateListOfFiles ();
				var filesListView = View.FindViewById<ListView> (Resource.Id.file_list);
				filesListView.Adapter = filesArrayAdapter;
			}
		}

		List<File> CreateListOfFiles ()
		{
			var listOfFiles = new List<File> ();
			AddFilesToList (listOfFiles, Activity.FilesDir);

			if (Utils.IsExternalStorageAvailable ())
				AddFilesToList (listOfFiles, Activity.GetExternalFilesDir (null));

			AddFilesToList (listOfFiles, Activity.NoBackupFilesDir);
			return listOfFiles;
		}

		void AddFilesToList (List<File> listOfFiles, File dir)
		{
			File[] files = dir.ListFiles ();
			foreach (File file in files)
				listOfFiles.Add (file);
		}

		void UpdateListOfFiles ()
		{
			var emptyFileListMessage = View.FindViewById<TextView> (Resource.Id.empty_file_list_message);
			files = CreateListOfFiles ();

			if (filesArrayAdapter.Count > 0) 
				filesArrayAdapter.Clear ();
			
			foreach (File file in files)
				filesArrayAdapter.Add (file);
			
			// Display a message instructing to add files if no files found.
			if (files.Count == 0)
				emptyFileListMessage.Visibility = ViewStates.Visible;
			else
				emptyFileListMessage.Visibility = ViewStates.Gone;
		}
	}

	class FileArrayAdapter : ArrayAdapter<File>
	{
		public FileArrayAdapter (Context context, int resource, IList<File> objects) : base (context, resource, objects)
		{
		}

		public override View GetView (int position, View convertView, ViewGroup parent)
		{
			var inflater = LayoutInflater.From (Context);
			View itemView = inflater.Inflate (Resource.Layout.file_list_item, parent, false);
			var fileNameView = itemView.FindViewById <TextView> (Resource.Id.file_name);
			var fileName = GetItem (position).AbsolutePath;
			fileNameView.Text = fileName;
			var fileSize = itemView.FindViewById <TextView> (Resource.Id.file_size);
			// TODO format bytes
			string fileSizeInBytes = GetItem (position).Length ().ToString ();
			fileSize.Text = fileSizeInBytes;
			return itemView;
		}

	}
}

