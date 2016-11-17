/*
Requires node.js and Curl to run.
Modes of operation: 
1) Increments version number, makes fresh build
- node build version=(major|minor|patch)

2) Packages mod, creates github release, uploads package. Release info is made from last commit comment.
- node build release

3) Release an older revision.
- node build release=34a9fe

4) Release an older revision, overriding the version number in the AssemblyInfo
- node build release=34a9fe override=1.2.1

This is the Windows only because MSBuild is used.
*/

var fs = require('fs');
var child_process = require('child_process');
var process = require('process');
var util = require('util');
var readline = require('readline');

var MSBuildPath = "C:/Windows/Microsoft.NET/Framework64/v4.0.30319/MSBuild.exe";
var sevenZipPath = "C:/Program Files/7-Zip/7z.exe";
var assemblyInfoPath = "./Properties/AssemblyInfo.cs";
var githubTokenPath = "../githubToken.txt";

//////////////////////////////////////////// UTILS ////////////////////////////////////////////

(function(){
	var styles = {bold:[1,22],black:[30,39],red:[31,39],green:[32,39],yellow:[33,39],white:[37,39],grey:[90,39],bgRed:[41,49],bgGreen:[42,49],bgWhite:[47,49]};
	Object.prototype.stylize = function(styleId){
		var val = styles[styleId];
		return '\u001b[' + val[0] + 'm' + this + '\u001b[' + val[1] + 'm';	
	}
}());

Array.prototype.forEach = function(iteratee) {
	var index = -1, length = this ? this.length : 0;
	while (++index < length) if (iteratee(this[index], index, this) === false) break;
	return this;
}

var runner = {
	runFailed: false,
	failReason: null,
	runTasks: function(tasks){
		for(i = 0; i < tasks.length; i++){			
			this.runFailed = false;
			this.failReason = null;
			item = tasks[i];
			console.log(('='.repeat(10)+" Running "+item.name+" "+'='.repeat(10)).stylize('bold'));
			try { item() }
			catch(err) {
				this.runFailed = true;
				this.failReason = err.stack.replace(/.+/, function(str){return str.stylize('bold')});
			}
			var resultStr = (this.runFailed?" FAILURE ".stylize('red'):" SUCCESS ".stylize('green')).stylize('bold');
			console.log(("Result: "+item.name+" - "+(this.runFailed?"FAILURE".stylize('red'):"SUCCESS").stylize('bold')).stylize(this.runFailed?'red':'green'));
			if(this.runFailed){
				if(this.reason!==null) console.log(("Reason: "+this.failReason).stylize('yellow'));
				if('cleanup' in this){
					this.cleanup();
				}
				break;
			}
		}
	},
	fail: function(message){
		this.runFailed = true;
		this.failReason = message.stylize('bold');
	}
}

function matchFileContents(path, pattern){
	var contents
	try {
		contents = fs.readFileSync(path).toString();
	} catch(err) {
		return null;
	}
	var match = contents.match(pattern);
	if(match!=null && match.length>1){
		return match[1];
	}
	return null;
}

function replaceMatchedCaptureInFile(path, pattern, replacement){
	var contents = fs.readFileSync(path).toString();
	var replacer = function(match, capture){
		return match.replace(capture, replacement);
	}
	var newContents = contents.replace(pattern, replacer);
	if(newContents != contents){
		fs.writeFileSync(path, newContents);
		return true;
	}
	return false;
}

function readAssemblyVersion(){
	if(forcedPublishVersion!==null){
		return forcedPublishVersion;
	}
	var versionStr;
	var overrideVersion = matchFileContents(versionFilePath, overrideVersionPattern); // try to get override version first
	if(overrideVersion !== null){
		versionStr = overrideVersion;
		overrideVersionActive = true;
	} else {
		var versionStr = matchFileContents(assemblyInfoPath, assemblyVersionPattern); // use assembly version otherwise
		if(versionStr === null) throw new Error("Invalid AssemblyInfo.cs contents!");
		overrideVersionActive = false;
	}
	versionParts = versionStr.split('.');
	versionParts.length = 3;
	return versionParts.join('.');
}

function modNameFromWorkingDirectory(){
	var cwdParts = workingDirectory.split('/');
	return cwdParts[cwdParts.length-1];
}

function quote(str){
	return '"'+str+'"';
}

function readAPIToken(path){
	try {
	var contents = fs.readFileSync(path).toString();
	} catch(err) {
		console.log(("Failed to read token file at "+path).stylize('red'))
	}
	return contents.trim();
}

function readUserInputSync(){
	var BUFSIZE=256;
	var buf = new Buffer(BUFSIZE);
	var bytesRead = 0;
	bytesRead = fs.readSync(process.stdin.fd, buf, 0, BUFSIZE);
	return buf.toString(null, 0, bytesRead).trim();
}

//////////////////////////////////////////// SETUP ////////////////////////////////////////////
var workingDirectory = process.cwd().replace(/\\/g, '/');
var modName = modNameFromWorkingDirectory();
var modDirPath = workingDirectory + "/Mods/" + modName
var versionFilePath = modDirPath + "/About/Version.xml";
var aboutFilePath = modDirPath + "/About/About.xml";
var apiToken = readAPIToken(githubTokenPath);
var currentVersion = null;
var assemblyVersionPattern = /\[assembly: AssemblyVersion\("((?:\d|\.)+?)"\)\]/;
var assemblyFileVersionPattern = /\[assembly: AssemblyFileVersion\("((?:\d|\.)+?)"\)\]/;
var overrideVersionPattern = /overrideVersion>([\d\.]+)/;
var githubRepoPattern = /gitHubRepository>([\w\/]+)/;
var aboutVersionPattern = /Version: ([\d\.]+)/;
var overrideVersionActive = false;

var revisionTypesEnum = {"major":0, "minor":1, "patch":2};
var execModesEnum = {"version":1, "release":2, "targetRelease":3};
var execMode = 0;
var revisionType = null;
var releaseRevision = null;
var forcedPublishVersion = null;

process.argv.forEach(function(arg, index){
	if(index<2) return; // skip exec path and js file
	var opt = arg;
	var value = null;
	if(arg.includes('=')){
		var parts = arg.split('=')
		opt = parts[0];
		value = parts[1];
	}	
	switch(opt){
		case 'version':
			if(revisionTypesEnum.hasOwnProperty(value)){
				execMode = execModesEnum.version;
				revisionType = revisionTypesEnum[value];
			}
		break;
		case 'release':
			if(value!==null){
				execMode = execModesEnum.targetRelease;
				releaseRevision = value;
			} else {
				execMode = execModesEnum.release;
			}
		break;
		case 'override':
			if(value!==null){
				forcedPublishVersion = value;
			}
		break;
	}
});

//////////////////////////////////////////// TASKS ////////////////////////////////////////////

function IncrementVersion(){
	var versionParts = currentVersion.split('.');
	switch(revisionType){
		case revisionTypesEnum.major:
			versionParts[0]++;
			versionParts[1] = versionParts[2] = 0;
		break;
		case revisionTypesEnum.minor:
			versionParts[1]++;
			versionParts[2] = 0;
		break;
		case revisionTypesEnum.patch:
			versionParts[2]++;
	}
	currentVersion = versionParts.join('.');
	console.log("New version is "+currentVersion);
}

function UpdateOverrideVersion(){
	if(!overrideVersionActive) {
		console.log("Override version inactive, skipping.");
		return;
	}
	replaceMatchedCaptureInFile(versionFilePath, overrideVersionPattern, currentVersion);
}

function UpdateAssemblyInfo(){
	if(overrideVersionActive) {
		console.log("Override version active, skipping.");
		return;
	}
	replaceMatchedCaptureInFile(assemblyInfoPath, assemblyVersionPattern, currentVersion);
	replaceMatchedCaptureInFile(assemblyInfoPath, assemblyFileVersionPattern, currentVersion);
}

function BuildAssembly(){
	if(overrideVersionActive) {
		console.log("Assembly info was not updated, skipping.");
		return;
	}
	if(!fs.existsSync(MSBuildPath)){
		runner.fail("Failed to find MSBuild at "+MSBuildPath);
		return;
	}
	var stdout;
	try {
		stdout = child_process.execSync(quote(MSBuildPath), [quote(process.cwd())]);
	} catch(err){
		runner.fail(err.stdout.toString());
	}
}

function UpdateAboutXmlVersion(){
	var replaced = replaceMatchedCaptureInFile(aboutFilePath, aboutVersionPattern, currentVersion);
	if(!replaced) console.log("About.xml version information not found, skipping.".stylize('yellow'));
}

var packageFilename;
var packagePath;

function PackageRelease(){
	if(!fs.existsSync(sevenZipPath)){
		runner.fail("Failed to find 7zip at "+sevenZipPath);
		return;
	}
	packageFilename = modName+"_"+currentVersion+".zip";
	packagePath = workingDirectory + '/' + packageFilename;
	var stdout;
	try {
		fs.unlinkSync(packagePath);
		console.log("Deleted existing package");
	} catch(err) {}
	try {
		stdout = child_process.execSync(quote(sevenZipPath) + " a -tzip "+quote(packagePath)+" "+quote(modDirPath));
		console.log("Created "+packagePath);
	} catch(err){
		runner.fail(err.stdout.toString());
	}
}

var commitMessage;

function FetchCommitMessage(){
	var stdout = child_process.execSync("git log -1 --pretty=%B");
	commitMessage = stdout.toString();
}

var releaseId;
var uploadUrl;

function MakeGithubRelease(){
	var repoPath = matchFileContents(versionFilePath, githubRepoPattern);
	if(repoPath === null){
		runner.fail("Could not parse repository path from version file: "+versionFilePath);
		return;
	}
	var commitLines = commitMessage.split('\n');
	var commitHeadline = commitLines[0];
	var otherLines = commitLines.filter(function(elem, idx){ return idx>0 && elem.length>0 }).join('\n');
	var commitish = releaseRevision === null ? "master" : releaseRevision;
	var payload = {
		tag_name: 'v'+currentVersion,
		target_commitish: commitish,
		name: commitLines[0],
		body: otherLines,
		draft: false,
		prerelease: false
	}
	var readable = util.inspect(payload).replace(/\\n/g, '\n');
	console.log(readable);
	process.stdout.write("Create a release with these settings? (y/n): ");
	var userInput = readUserInputSync();
	if(userInput.toLowerCase()!="y"){
		runner.fail("User aborted release");
		return;
	}
	var jsonStr = JSON.stringify(payload);
	var requestUrl = "https://api.github.com/repos/"+repoPath+"/releases?access_token="+apiToken;
	var response;
	try {
		response = child_process.execFileSync("curl", [requestUrl, '-s', '--data', jsonStr]).toString();
	} catch(err){
		runner.fail("Curl failure: "+err.toString());
	}
	var parsedResponse = JSON.parse(response);
	if(!('id' in parsedResponse)){
		runner.fail("Creating release failed:\n"+response);
	}
	releaseId = parsedResponse.id;
	uploadUrl = parsedResponse.upload_url;
	console.log("Created release with id "+releaseId);
}

function UploadReleasePackage(){
	var fileSize = fs.statSync(packagePath).size;
	uploadUrl = uploadUrl.replace("{?name,label}", "?name="+encodeURIComponent(packageFilename)+"&size="+fileSize);
	var response;
	try {
		response = child_process.execFileSync("curl", [uploadUrl, '-s', '-H', "Authorization:token "+apiToken, '-H', 'Content-Type:application/zip', '--data-binary', "@"+packageFilename]).toString();
	} catch(err){
		runner.fail("Curl failure: "+err.toString());
	}
	var parsedResponse = JSON.parse(response);
	if(!('browser_download_url' in parsedResponse)){
		runner.fail("Package upload failed:\n"+response);
	}
	console.log("Uploaded package: "+parsedResponse.browser_download_url);		
}

function Cleanup(){
	fs.unlinkSync(packagePath);
}

function CheckoutRevision(){
	var stdout = child_process.execSync("git checkout "+releaseRevision);
	currentVersion = readAssemblyVersion();
}

function CheckoutMaster(){
	var stdout = child_process.execSync("git checkout master");
	currentVersion = readAssemblyVersion();
}

function CleanupAfterFailure(){
	if(typeof packagePath!==undefined){
		fs.unlinkSync(packagePath);
	}
	if(releaseRevision!==null){
		child_process.execSync("git checkout master");
	}
}

//////////////////////////////////////////// EXECUTION ////////////////////////////////////////////

if(execMode == 0){
	console.log("Usage: node build version=(major|minor|patch)".stylize('red'));
	console.log("Or: node build release".stylize('red'));
	console.log("Or: node build release=(commit hash)".stylize('red'));
	console.log("Or: node build release=(commit hash) override=1.2.1".stylize('red'));
} else {
	runner.cleanup = CleanupAfterFailure;
	currentVersion = readAssemblyVersion();
	if(execMode == execModesEnum.version){
		runner.runTasks([IncrementVersion, UpdateOverrideVersion, UpdateAboutXmlVersion, UpdateAssemblyInfo, BuildAssembly])
	} else if(execMode == execModesEnum.release){
		runner.runTasks([PackageRelease, FetchCommitMessage, MakeGithubRelease, UploadReleasePackage, Cleanup])
	} else if(execMode == execModesEnum.targetRelease){
		runner.runTasks([CheckoutRevision, PackageRelease, FetchCommitMessage, MakeGithubRelease, UploadReleasePackage, Cleanup, CheckoutMaster])
	}
}