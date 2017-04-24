/// <reference path="Site.js" />

function FileSelected() {
  var fileCtl = GetControlByTypeAndId("input", "uxImportContourGisFile");
  var count = fileCtl.files.length;
  var detsCtl = document.getElementById('details');
  detsCtl.innerHTML = "";
  for (var index = 0; index < count; index++) {
    var file = fileCtl.files[index];
    //console.log(file.name.endsWith(".zip").toString());
    //if (file.type.toLowerCase() != "application/zip" && file.type.toLowerCase() != "application/x-zip-compressed") {
    if (!file.name.endsWith(".zip") || file.type.toLowerCase().indexOf("zip") < 0) {
      HiUser("Please select a '.zip' file."); fileCtl.value = ""; return false;
    }
    var fileSize = 0;
    if (file.size > 1024 * 1024) fileSize = (Math.round(file.size * 100 / (1024 * 1024)) / 100).toString() + 'MB';
    else fileSize = (Math.round(file.size * 100 / 1024) / 100).toString() + 'KB';
//    detsCtl.innerHTML += 'Name: ' + file.name + '<br>Size: ' + fileSize + '<br>Type: ' + file.type;
    detsCtl.innerHTML += 'Size: ' + fileSize + '<br>Type: ' + file.type;
  }
}
function UploadFile(sendr, type) {
  try {
    var fileCtl = GetControlByTypeAndId("input", "uxImportContourGisFile");
    var count = fileCtl.files.length;
    if (count === 0) { HiUser("Please select a zip file first."); return false; }
    if (CheckExtension(fileCtl, "zip") === false) { /* alerts user in CheckExtension */ fileCtl.value = ""; return false; }

    var prjId = GetProjId();
    var fd = new FormData();
    for (var index = 0; index < count; index++) {
        var file = fileCtl.files[index];
        fd.append(file.name, file);
    }
    SetWebServiceIndicators(true, "Uploading File");
    var xhr = new XMLHttpRequest();
    xhr.upload.addEventListener("progress", UploadProgress, false);
    xhr.addEventListener("load", UploadComplete, false);
    xhr.addEventListener("error", UploadFailed, false);
    xhr.addEventListener("abort", UploadCanceled, false);
    xhr.open("POST", "/members/uploaddata.aspx?prjId=" + prjId, false);
    xhr.send(fd);
  } catch (e) { HiUser(e, "Upload File"); }
  return false;
}
function UploadProgress(evt) {
  if (evt.lengthComputable) {
    var percentComplete = Math.round(evt.loaded * 100 / evt.total);
    document.getElementById('progress').innerHTML = percentComplete.toString() + '%';
  }
  else {
    document.getElementById('progress').innerHTML = 'unable to compute file length';
  }
}
var someEvt;
function UploadComplete(evt) {
  someEvt = evt;
  var resp = evt.target.response; //get returned html from page
  var div = document.createElement("div"); //container for response
  div.innerHTML = resp;
  ShowResults(div);
  /* This event is raised when the server send back a response */
//  alert("Upload successful!");
  window.close();
  SetWebServiceIndicators(false);
}
function UploadFailed(evt) {
  alert("There was an error attempting to upload the file.");
  SetWebServiceIndicators(false);
}
function UploadCanceled(evt) {
  alert("The upload has been canceled by the user or the browser dropped the connection.");
  SetWebServiceIndicators(false);
}
function ShowResults(div) {
  SetWebServiceIndicators(false);
  var info, shpType, numRows, numCols, cols, fileNme;
  var ctls = div.getElementsByTagName("input");
  var ctl, ctlLen = ctls.length;

  for (var ctlIx = 0; ctlIx < ctlLen; ctlIx++) {
    ctl = ctls[ctlIx];
    if (ctl.id == "uxInfo") info=ctl.value;
    else if (ctl.id == "uxShapeType") shpType=ctl.value;
    else if (ctl.id == "uxRowCount") numRows = ctl.value;
    else if (ctl.id == "uxColCount") numCols = ctl.value;
    else if (ctl.id == "uxColumnNames") cols = ctl.value;
    else if (ctl.id == "uxShapefileName") fileNme = ctl.value;
  }
  if (shpType.trim().length <= 0) shpType = 0;
  if (numRows.trim().length <= 0) numRows = 0;
  if (numCols.trim().length <= 0) numCols = 0;
  var msg = CR + numRows + " rows with " + numCols + " columns of type " + shpType + " uploaded." + CR + cols + CR;
  if (info.trim().length > 0) msg += CR + "More info: " + CR + info + CR;
  //HiUser(msg, "GIS upload success");

  if (shpType.toLowerCase().indexOf("linestring") < 0) {
    HiUser("Uploaded shapefile was not of type linestring. Please select a new file.");
    return;
  }
  var recCnt = document.getElementById("uxImportContourRecordCount");
  if (parseInt(recCnt,10)< 1) {
    HiUser("Uploaded shapefile has no records. Please select a new file.");
    return;
  }
  recCnt.innerHTML = numRows + " records in uploaded shapefile.";

  var idSel = document.getElementById("uxImportContourContourColumn");
  idSel.options.length = 1; //reset
  var colNames = cols.split("|");
  colNames.sort();
  var colLen = colNames.length;
  var opt;
  for (var colX = 0; colX < colLen; colX++) {
    opt = document.createElement("option");
    opt.value = colNames[colX].toUpperCase();
    opt.innerHTML = colNames[colX];
    idSel.appendChild(opt);
    //opt = document.createElement("option");
    //opt.value = colNames[colX];
    //opt.innerHTML = colNames[colX];
    //subidSel.appendChild(opt);
  }
  //attempt defaults
  SetDropdownSelectedValueBySel(idSel, 'CONTOUR');

  document.getElementById("uxHiddenImportContourFileName").value = fileNme;
  document.getElementById("uxImportContourImport").removeAttribute("disabled"); 

  $("[data-import]").filter("[data-import='columns']").removeClass("display-none");
  $("[data-import]").filter("[data-import='import']").removeClass("display-none");   
}