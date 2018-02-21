﻿using cdcrush.lib;
using cdcrush.lib.app;
using cdcrush.lib.task;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cdcrush.prog
{
	
	
// Every Crush Job runs with these input parameters
public struct CrushParams
{	
	public string inputFile;	// The CUE file to compress
	public string outputDir;	// Output Directory.
	public string cdTitle;		// Custom CD TITLE
	public int audioQuality;	// OGG quality, Check FFmpeg class
	public string cover;		// Cover image for the CD, square
	// : Internal Use :

	public int crushedSize {get; internal set;}

	// Temp dir for the current batch, it's autoset by this object,
	// is a subfolder of the master TEMP folder
	internal string tempDir;
	// Final destination ARC file, autogenerated from CD TITLE
	internal string finalArcPath;
	internal CueReader cd;
	internal bool workFromTemp; // True if the original/bin has more than one track and is cut into the temp folder
}// --

class JobCrush:CJob
{

	public JobCrush(CrushParams p):base("Compress CD")
	{
		// Check for input files
		// :: --------------------
			if(!CDCRUSH.check_file_(p.inputFile,".cue")) {
				fail(msg:CDCRUSH.ERROR);
				return;
			}

			if(string.IsNullOrEmpty(p.outputDir)) {
				p.outputDir = Path.GetDirectoryName(p.inputFile);
			}

			if(!FileTools.createDirectory(p.outputDir)) {
				fail(msg: "Can't create Output Dir " + p.outputDir);
				return;
			}

			p.tempDir = Path.Combine(CDCRUSH.TEMP_FOLDER,Guid.NewGuid().ToString().Substring(0, 12));
			if(!FileTools.createDirectory(p.tempDir)) {
				fail(msg: "Can't create TEMP dir");
				return;
			}

		// IMPORTANT!! sharedData gets set by value, NOT A POINTER, do not make changes to p after this
		jobData = p;

		// --
		// - Read the CUE file ::
		add(new CTask((t) =>
		{
			var cd = new CueReader();
			jobData.cd = cd;

			if(!cd.load(p.inputFile)) {
				t.fail(msg:cd.ERROR);
				return;
			}

			// Post CD CUE load ::

			if(!string.IsNullOrWhiteSpace(p.cdTitle)) // valid:
			{
				cd.CD_TITLE = FileTools.sanitizeFilename(p.cdTitle);
			}

			// Real quality to string name
			cd.CD_AUDIO_QUALITY = FFmpeg.QUALITY[p.audioQuality].ToString() + "kbps";

			// This flag notes that all files will go to the TEMP folder
			jobData.workFromTemp = !cd.MULTIFILE;

			// Generate the final arc name now that I have the CD TITLE
			jobData.finalArcPath = Path.Combine(p.outputDir, cd.CD_TITLE + ".arc");

			t.complete();

		},"Reading",true));

		
		// - Cut tracks if it has to
		// ---------------------------
		add(new TaskCutTrackFiles());

		// - Compress tracks
		// ---------------------
		add(new CTask((t) =>
		{
			CueReader cd = jobData.cd;
			foreach(CueTrack tr in cd.tracks) {
				addNextAsync(new TaskCompressTrack(tr));
			}//--
			t.complete();
		},"Preparing"));


		// Create Archive
		// Add all tracks to the final archive
		// ---------------------
		add(new CTask((t) =>
		{
			CueReader cd = jobData.cd;

			// -- Get list of files::
			System.Collections.ArrayList files = new System.Collections.ArrayList();
			foreach(var tr in cd.tracks){
				files.Add(tr.workingFile); // Working file is valid, was set earlier
			}

			var arc = new FreeArc();
			t.handleCliReport(arc);
			arc.compress((string[])files.ToArray(typeof( string )), jobData.finalArcPath);

		}, "Compressing"));


		// - Create CD SETTINGS and push it to the final archive
		// ( I am appending these files so that they can be quickly loaded later )
		// --------------------
		add(new CTask((t) =>
		{
			CueReader cd = jobData.cd;

			// #DEBUG
			//cd.debugInfo();

			string path_settings = Path.Combine(p.tempDir, CDCRUSH.CDCRUSH_SETTINGS);
			if(!cd.saveJson(path_settings))
			{
				t.fail(msg: cd.ERROR);
				return;
			}
	
			// - Cover Image Set?
			string path_cover;
			if(p.cover!=null) {
				path_cover = Path.Combine(p.tempDir,CDCRUSH.CDCRUSH_COVER);
				File.Copy(p.cover,path_cover);
			}else {
				path_cover = null;
			}

			// - Append the file(s)
			var arc = new FreeArc();
			t.handleCliReport(arc);
			arc.appendFiles(new string[]{path_settings, path_cover},jobData.finalArcPath);

		}, "Finalizing"));

		// - Get post data
		// - Clean files
		add(new CTask((t) =>
		{
			var finfo = new FileInfo(jobData.finalArcPath);
			jobData.crushedSize = (int)finfo.Length;

			// - Cleanup
			if (p.tempDir != p.outputDir)
			{
				// NOTE: This is always a subdir of the master
				Directory.Delete(p.tempDir, true);
			}// --

			t.complete();

		},"Finalizing"));

		// -- COMPLETE --
		
	}// -----------------------------------------


}// --
}// --
