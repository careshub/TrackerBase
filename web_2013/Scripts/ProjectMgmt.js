/// <reference path="http://ajax.aspnetcdn.com/ajax/jQuery/jquery-1.5-vsdoc.js"/>
/// <reference path="Site.js" />
var currProjectId;
var currProjectName;
var currRowSelect = -1; //select element id
var currProjectAccordion = 0;
var noProjMsg = "Please select a project using the radio buttons.";
var projects;
var selectedProjectId;
var minPrjYr = 2010;//default
var maxPrjYr = 2020; //default
var projIds = []; //pre-creation ids to allow selection of new project

function contentDocReady() { //called from Master $(function ()
}
//runs on both sync and async postbacks......
function contentPageLoad() {
  //  if (HasClass(GetControlByTypeAndId("div", "uxCreateProjectContainer"), 'display-none')) { //avoid msg on state change when creating new project
  try {
    projects = projectsJson.d; //set as if web service call
  } catch (e) { HiUser(e, "Set projects"); }
  LoadProjectsDone();
  jQuery('body').removeClass('js');
  $(".accordion").accordion();
  $("#uxProjectsContainer .accordion.collapsible.collapsed").first().accordion({ collapsible: true, active: 0 });
  $("#uxProjectsContainer .accordion.collapsible.collapsed").slice(1).accordion({ collapsible: true, active: false });
  $('input:radio[name*="ProjectSelect"]').on('click', function (e) { SelectProject(this, e); });
  //    $('[id$=uxProjectsStartMonth]').each(function () { var mon = parseInt($(this).text()); $(this).text(monthsAbbr[mon]) });
  $('[id*=uxProjectStartMonth]').each(function () { var mon = parseInt($(this).text()); $(this).text(monthsAbbr[mon]) });
  ClearProjectSelection();
  SelectProjectById(GetProjId());
  maxPrjYr = GetCurrentYear() + 5;
  minPrjYr = maxPrjYr - 10;
  //  }
}
function GetMinYearForProjects() { return minPrjYr; }
function GetMaxYearForProjects() { return maxPrjYr; }
function FillStates() {
  try {
    var sel = GetControlByTypeAndId("select", "uxCreateProjectState");
    if (sel.length !== 52) {
      sel.length = 0;
      var opt = document.createElement("option");
      opt.value = -1;
      opt.text = "-- SELECT --";
      sel.appendChild(opt);
      var states2 = states.split("|"), sLen = states2.length;
      for (var i = 0; i < sLen; i++) {
        if (states2[i].toLowerCase() != "puerto rico") {
          var opt = document.createElement("option");
          opt.value = states2[i];
          opt.text = states2[i];
          sel.appendChild(opt);
        }
      }
    }
  } catch (e) { HiUser(e, "Fill States"); }
}
function FillCountys(sendr) {
  try {
    var state = sendr.options[sendr.selectedIndex].value;
    var svcData = {};
    svcData["state"] = state;
    $.ajax({
      url: "GISTools.asmx/GetCounties"
      , data: JSON.stringify(svcData)
    })
    .done(function (msg) {
      var retVal = msg.d;
      var errIdx = retVal.indexOf("||");
      if (errIdx > -1) {
        var error = retVal.slice(errIdx).replace("||", ""); //get error
        HiUser(error, "Get Counties");
        retVal = retVal.replace(error, "").replace("||", ""); //remove error part
      }
      var counties = retVal.split("|");
      var sel = GetControlByTypeAndId("select", "uxCreateProjectCounty");
      sel.length = 0;
      var opt = document.createElement("option");
      opt.value = -1;
      opt.text = "-- SELECT COUNTY --";
      sel.appendChild(opt);
      var cLen = counties.length;
      for (var i = 0; i < cLen; i++) {
        var opt = document.createElement("option");
        opt.value = counties[i];
        opt.text = counties[i].slice(counties[i].indexOf(" "));
        sel.appendChild(opt);
      }
    })
    .fail(function (jqXHR, textStatus, errorThrown) {
      closeForm = false;
      var errorResult = errorThrown;
      HiUser(errorResult, "Get Counties failed.");
    })
    .always(function () {
    });
  } catch (e) { HiUser(e, "Fill Counties"); }
}
function CreateNewProject() {
  try {
    SetEditCalYears(false);
    SetEditCalMonth(false);
    OpenForm('uxCreateProject');
    $('[id$=uxCreateProjectName]').focus();
  } catch (e) { HiUser(e, "Create New Project"); }
}
function SubmitCreateProject() {
  try {
    var $name = $("[id$=uxCreateProjectName]");
    if ("" == $name.val().trim()) {
      alert("Please enter a project name.");
      $name.focus();
      return false;
    }
    else if (!IsUniqueProjectName($name.val().trim())) { alert("Please enter a UNIQUE project name."); return false; }
    else { //verify state and county sel'd
      var state = $("[id$=uxCreateProjectState]").val();
      var cnty = $("[id$=uxCreateProjectCounty]").val();
      if ("-1" == state || "-1" == cnty) { alert("Please select a state and county for your project."); return false; }
    }
    $("[id$=uxHiddenProjectName]").val($("[id$=uxCreateProjectName]").val().trim());
    SubmitProject("Create");
  } catch (e) { alert(e); return false; }
}
function ValidateProjectName(sendr) {
  try {
    var desc = sendr.id + "Desc";
    desc = desc.substring(desc.lastIndexOf("_") + 1);
    var regex;
    var obj = $(sendr);
    var objtext = obj.val().trim();
    if (objtext.length == 0) return;

    var objtext1 = objtext.substring(0, 1);
    var objtext2 = objtext.substring(1);
    regex = /[^a-z]/i; // letters, case-insensitive
    var objtext1regex = objtext1.replace(regex, "");

    regex = /[^a-z0-9-_]/gi;  // alphanum, dash, underscore, global, case-insensitive
    var objtext2regex = objtext2.replace(regex, "");

    var newobjtext = objtext1regex.toString() + objtext2regex.toString();
    if (objtext != newobjtext) $("#" + desc).addClass("notes"); //show validation message
    obj.val(newobjtext); //.replace(/\//g, 'ForwardSlash'))
  } catch (e) { alert(e); }
}
function ClearProjectSelection() {
  try {
    currRowSelect = -1;
    currProjectId = null;
    currProjectName = null;
    var inputs = $("[id$=uxProjectsContainer] :input");
    inputs.prop("checked", false);
    inputs.parent().removeClass("accord-header-highlight");
  } catch (e) { alert(e); }
}
function GetSelectedProjectId() {
  var retVal = "";
  try {
    if (-1 == selectedProjectId) return "";
    var idCtlName = selectedProjectId.replace("ProjectSelect", "OperationGuid");
    var idCtl = $("#" + idCtlName + "");
    if (1 > idCtl.length) return ""; //not found
    retVal = idCtl.val();
  } catch (e) { HiUser(e, "Get Selected Project Id"); }
  return retVal;
}
function GetSelectedProjectOperationId() {
  var retVal = "";
  try {
    if (-1 == selectedProjectId) return "";
    var idCtlName = selectedProjectId.replace("ProjectSelect", "OperationId");
    var idCtl = $("#" + idCtlName + "");
    if (1 > idCtl.length) return ""; //not found
    retVal = idCtl.val();
  } catch (e) { HiUser(e, "Get Selected Project Operation Id"); }
  return retVal;
}
function GetSelectedProjectStartCalYear() {
  var retVal = null;
  var prjs = projectsJson.projects, opRec, opDatum;
  var selId = GetSelectedProjectId();
  var calYr = null;
  try {
    for (var prj in prjs) {
      opRec = prjs[prj].Operation;
      opDatum = prjs[prj].OperationDatum;
      if (opRec && opDatum && opDatum.GUID.toString().toLowerCase() === selId.toLowerCase()) {
        calYr = opRec.StartCalYear;
        if (calYr) retVal = calYr;
      }
    }
  } catch (e) { HiUser(e, "Get Selected Project Start Cal Year"); }
  return retVal;
}
function GetSelectedProjectStartCalMonth() {
  var retVal = null;
  var prjs = projectsJson.projects, opRec, opDatum;
  var selId = GetSelectedProjectId();
  var calYr = null;
  try {
    for (var prj in prjs) {
      opRec = prjs[prj].Operation;
      opDatum = prjs[prj].OperationDatum;
      if (opRec && opDatum && opDatum.GUID.toString().toLowerCase() === selId.toLowerCase()) {
        calYr = opRec.StartCalMonth;
        if (calYr) retVal = calYr;
      }
    }
  } catch (e) { HiUser(e, "Get Selected Project Start Cal Month"); }
  return retVal;
}
function SelectProject(sendr, e) {
  try {
    //validate new selection
    var isChecked = sendr.checked;
    if (isChecked && sendr.id === currRowSelect) return; //no change
    var $sendr = $(sendr);
    var sendrId = sendr.id;
    //turn off other selections, reset css
    $('#uxProjectsContainer input[type="radio"]:not(#' + sendrId + ')').prop("checked", false);
    $('#uxProjectsContainer input[type="radio"]:not(#' + sendrId + ')').parent().removeClass("accord-header-highlight");
    //highlight selection?
    $sendr.parent().addClass("accord-header-highlight");

    e.stopPropagation(); //stopPropagation or else radio button becomes unselected

    currRowSelect = sendrId;
    currProjectId = $("#" + currRowSelect.replace("uxProjectSelect", "uxProjectProjectId")).val();
    var prjNameCtrlName = sendr.id.replace("uxProjectSelect", "uxProjectName");
    currProjectName = $("#" + prjNameCtrlName).html();
    SetProjId(currProjectId);
    $("[id$=uxHiddenProjectName]").val(currProjectName);
    selectedProjectId = sendrId;
    $.observable(projectsJson).setProperty("selectedID", GetSelectedProjectId());
  } catch (e) { alert(e); }
}
function CanOpenProject() {
  try {
    if (-1 === currRowSelect) { alert(noProjMsg); return false; }
    SetPageFlag('stepone');
    SetProjId(currProjectId);
    Redirect('/ProjectHome');
  } catch (e) { alert(e); }
}
function CanDeleteProject() {
  try {
    if (-1 === currRowSelect) { alert(noProjMsg); return false; }
    return confirm('Please confirm: do you really want to delete project ' + $("[id$=uxHiddenProjectName]").val() + '?');
  } catch (e) { alert(e); }
}
function SetImportProject() {
  try {
    if (-1 === currRowSelect) { alert(noProjMsg); return false; }
    OpenForm('uxImportTools');
  } catch (e) { alert(e); }
}
function SetExportProject() {
  try {
    if (-1 === currRowSelect) { alert(noProjMsg); return false; }
    OpenForm('uxDownloadTools');
  } catch (e) { alert(e); }
}
function SubmitDownload() {
  try {
    var prjId = GetControlByTypeAndId("input", "uxHiddenProjectId").value;
    var prjName = GetControlByTypeAndId("input", "uxHiddenProjectName").value;
    if (prjId.length === 0) alert("No project id found.");
    else {
      var coverCtrl = GetControlByTypeAndId("div", "uxDownloadToolsForm");
      var files = "";
      var holder = $("#uxDownloadToolsMain");
      if (holder.find("#uxExportGis").is(':checked')) files += "gis|";
      if (holder.find("#uxExportGisXml").is(':checked')) files += "gisxml|";
      if (holder.find("#uxExportClipper").is(':checked')) files += "clipper|";
      if (files.trim().length < 1) { RemoveClass(GetControlByTypeAndId("p", "uxOneFileWarning"), "display-none"); return; }
      else AddClass(GetControlByTypeAndId("p", "uxOneFileWarning"), "display-none");
      //  Public Function DumpData(ByVal projectId As Integer, ByVal projectName As String, ByVal files As String) As ReturnProjectExport
      SetWebServiceIndicators(true, "Exporting Project");
      GISTools.DumpData(prjId, prjName, files,
          function (data) {
            if (data.fileName) {
              var zipFile = data.fileName;
              GetControlByTypeAndId("label", "uxDownloadZipLinkHolder").innerHTML = zipFile.substring(zipFile.lastIndexOf("/") + 1).replace(prjName.toLowerCase(), prjName).link(zipFile);
              RemoveClass(GetControlByTypeAndId("p", "uxDownloadZipHolder"), "display-none");
            }
            if (data.info) HiUser(data.info.replaceAll("<br />", CR), "Export Project web service call");
            SetWebServiceIndicators(false, coverCtrl);
          } // success function);
          , function (error, context, methodName) {
            try {
              var stackTrace = error.get_stackTrace();
              var message = error.get_message();
              var statusCode = error.get_statusCode();
              var exceptionType = error.get_exceptionType();
              var timedout = error.get_timedOut();
              alert("Stack Trace: " + stackTrace + "\r\n" +
                  "Service Error: " + message + "\r\n" +
                  "Status Code: " + statusCode + "\r\n" +
                  "Exception Type: " + exceptionType + "\r\n" +
                  "Timedout: " + timedout);
            } catch (e) { HiUser("SubmitDownload callback failed error: " + e, "Export Project web service call failed"); SetWebServiceIndicators(false, coverCtrl); }
            SetWebServiceIndicators(false, coverCtrl);
          }
      //) { HiUser("Project download failed." + result.toString, "Submit Download"); }
      ) //webservice
    }
  } catch (e) { alert("SubmitDownload: " + e); SetWebServiceIndicators(false, coverCtrl); }
}

function EditProject() {
  try {
    if (-1 === currRowSelect) { alert(noProjMsg); return false; }
    OpenForm('uxEditProject');
    SetEditCalYears(true);
    SetEditCalMonth(true);
    //    $("[id$=uxEditProjectName]").val(currProjectName);
    $("[id$=uxEditProjectName]").focus();
  } catch (e) { alert(e); }
}
function SetEditCalYears(editing) {
  try {
    var prjYr;
    var minYr = GetMinYearForProjects(), maxYr = GetMaxYearForProjects();
    var selId = (editing === true) ? "uxEditProjectStartCalYear" : "uxCreateProjectStartCalYear";
    var optn, $sel = $("#" + selId + "");
    $sel.empty();

    if (editing === true) {
      prjYr = GetSelectedProjectStartCalYear();
      if (prjYr) {
        prjYr = parseInt(prjYr);
        if (prjYr < minYr) minYr = prjYr;
        if (prjYr > maxYr) maxYr = prjYr;
      }
    }
    for (var yr = minYr; yr <= maxYr; yr++) {
      optn = document.createElement("OPTION");
      optn.text = yr;
      optn.value = yr;
      $sel.append(optn);
    }
    if (editing === true) { if (prjYr) $sel.val(prjYr); }
    else $sel.val(GetCurrentYear().toString());
  } catch (e) { HiUser(e, "Set Calendar Years"); }
}
function SetEditCalMonth(editing) {
  try {
    var prjMo;
    var selId = (editing === true) ? "uxEditProjectStartCalMonth" : "uxCreateProjectStartCalMonth";
    var $sel = $("#" + selId + "");

    if (editing === true) {
      prjMo = GetSelectedProjectStartCalMonth();
      if (prjMo) { prjMo = parseInt(prjMo); }
    }
    if (editing === true) { if (prjMo) $sel.val(prjMo); }
    else $sel.val((1 + GetCurrentMonth()).toString());
  } catch (e) { HiUser(e, "Set Calendar Month"); }
}
function LoadProjects() {
  try {
    $("[id$=uxNoProjects]").html("");
    var infoMsg = "";
    SetWebServiceIndicators(true, "Getting projects");
    $.ajax({
      url: "GISTools.asmx/GetProjects"
    })
    .done(function (msg) {
      projects = msg.d;
      if (projects.info && projects.info.length > 0) HiUser(projects.info, "Get Projects succeeded");
      LoadProjectsDone();
    })
    .fail(function (jqXHR, textStatus, errorThrown) {
      var errorResult = errorThrown;
      HiUser(errorResult, "Get Projects failed.");
    })
    .always(function () {
      SetWebServiceIndicators(false);
      ClearProjectSelection();
      $("#uxProjectsInfo").html(infoMsg); //set after linking or it doesn't exist
    });
    //    projectsRetrievedIndx = editingFeatIndx; //store field indx
  } catch (err) { HiUser(err, "Load Projects"); SetWebServiceIndicators(false); }
}
function LoadProjectsDone() {
  try {
    if (!projects || !projects.projects || projects.projects.length === 0) $("[id$=uxNoProjects]").html("You don't have any projects created. Use the button to create a new project.");
    else { $("[id$=uxNoProjects]").html(""); ShowProjects(); }
  } catch (e) { HiUser(e, "Load Projects Done"); }
}
function ShowProjects() {
  try {
    projectsJsonTests = projects.projects;

    projectsJson = {
      projects: projectsJsonTests,
      selectedID: projectsJsonTests[0].OperationDatum.GUID,
      selected: function () {
        for (var i = 0; i < projectsJsonTests.length; i++) {
          if (projectsJsonTests[i].OperationDatum.GUID === this.selectedID) {
            return projectsJsonTests[i];
          }
        }
        return {};
      }
    };
    projectsJson.selected.depends = "selectedID";

    projectsTmpl.link("#uxProjectsContainer", projectsJson);
    SetAccordions();
    $('input:radio[name*="ProjectSelect"]').on('click', function (e) { SelectProject(this, e); });
  } catch (e) { HiUser(e, "Show Projects"); }
}

function GetNewProjectId() {
  try {
    var prj, curr;
    for (var prjIdx in projects.projects) {
      if (projects.projects.hasOwnProperty(prjIdx)) {
        prj = projects.projects[prjIdx];
        prjRec = prj.Project;
        if (!prjRec) continue;
        curr = prjRec.ObjectID;
        if (projIds.indexOf(curr) < 0) return curr;
      }
    }
    return null;
  } catch (e) { HiUser(e, "Get New Project Id"); }
}
function GetProjectIds() {
  try {
    var prj, curr;
    for (var prjIdx in projects.projects) {
      if (projects.projects.hasOwnProperty(prjIdx)) {
        prj = projects.projects[prjIdx];
        prjRec = prj.Project;
        if (!prjRec) continue;
        curr = prjRec.ObjectID;
        projIds.push(curr);
      }
    }
  } catch (e) { HiUser(e, "Get Project Ids"); }
}
function IsUniqueProjectName(prjName, idToIgnore) {
  try {
    var prj, curr;
    for (var prjIdx in projects.projects) {
      if (projects.projects.hasOwnProperty(prjIdx)) {
        prj = projects.projects[prjIdx];
        prjRec = prj.Project;
        if (!prjRec) continue;
        if (idToIgnore && prjRec.ObjectID == idToIgnore) continue;
        curr = prjRec.Name;
        if (curr.toLowerCase() === prjName.toLowerCase()) return false;
      }
    }
    return true;
  } catch (e) { HiUser(e, "Is Unique Project Name"); }
}
function SubmitEditProject() {
  try {
    var prjName = $("[id$=uxEditProjectName]").val().trim();
    if ("" == prjName) { alert("Please enter a project name."); return false; }
    if (!IsUniqueProjectName(prjName, currProjectId)) { alert("Please enter a UNIQUE project name."); return false; }
    var okToSubmit = true;
    if (true === okToSubmit) SubmitProject("Edit");
  } catch (e) { HiUser(e, "Submit Edit Project"); }
}
function SubmitProject(action) {
  try {
    GetProjectIds();
    var projid = GetProjId();
    var projectData = GetProjectForWebService(action);
    if (null === projectData) return;
    var svcData = {};
    if ("Edit" === action) svcData["projectId"] = parseInt(projid);
    svcData["projectdata"] = projectData;
    var infoMsg = "";
    var closeForm = true;
    var prjName = $("[id$=uxCreateProjectName]").val().trim();
    SetWebServiceIndicators(true, "Submitting Project");
    if ("Create" === action) {
      $.ajax({
        url: "GISTools.asmx/AddProject"
      , data: JSON.stringify(svcData)
      })
      .done(function (msg) {
        projects = msg.d;
        if (projects.info && projects.info.length > 0) HiUser(projects.info, "Create project succeeded");
        if (!projects || !projects.projects || projects.projects.length === 0) $("[id$=uxNoProjects]").html("You don't have any projects created. Use the button to create a new project.");
        else { $("[id$=uxNoProjects]").html(""); ShowProjects(); /*OpenNewProject(prjName);*/ }
      })
      .fail(function (jqXHR, textStatus, errorThrown) {
        closeForm = false;
        var errorResult = errorThrown;
        HiUser(errorResult, "Create Project failed.");
      })
      .always(function () {
        FinishCreateProject(closeForm, action, infoMsg);
      });
    } else {
      $.ajax({
        url: "GISTools.asmx/EditProject"
      , data: JSON.stringify(svcData)
      })
      .done(function (msg) {
        projects = msg.d;
        if (projects.info && projects.info.length > 0) HiUser(projects.info, "Edit project succeeded");
        if (!projects || !projects.projects || projects.projects.length === 0) $("[id$=uxNoProjects]").html("You don't have any projects created. Use the button to create a new project.");
        else { $("[id$=uxNoProjects]").html(""); ShowProjects(); }
      })
      .fail(function (jqXHR, textStatus, errorThrown) {
        closeForm = false;
        var errorResult = errorThrown;
        HiUser(errorResult, "Edit Project failed.");
      })
      .always(function () {
        FinishEditProject(closeForm, action, infoMsg);
      });
    }
    //    projectsRetrievedIndx = editingFeatIndx; //store project indx
  } catch (err) { HiUser(err, "Submit Project"); }
}
function FinishCreateProject(closeForm, action, infoMsg) {
  SetWebServiceIndicators(false);
  ClearProjectSelection();
  if (closeForm) CloseForm("ux" + action + "Project");

  var $info = $("#uxProjectsInfo");
  if ($info.length > 0) $info.html(infoMsg); //set after linking to template or it doesn't exist
  var newId = GetNewProjectId();
  SelectProjectById(newId);
}
function FinishEditProject(closeForm, action, infoMsg) {
  SetWebServiceIndicators(false);
  //  ClearProjectSelection();//no effect since reloading all
  var $info = $("#uxProjectsInfo");
  if ($info.length > 0) $info.html(infoMsg); //set after linking to template or it doesn't exist
  if (closeForm) CloseForm("ux" + action + "Project");
}
function SelectProjectById(oid) {
  try {
    var els = $("[id^='uxProjectProjectId']");
    els.each(function (index) {
      var sel, el = $(this);
      if (el.val() == oid) {
        sel = $(document.getElementById(el.attr('id').replace('uxProjectProjectId', 'uxProjectSelect')));
        sel.click();
      }
    });
  } catch (err) { HiUser(err, "Select Project By Id"); }
}
function OpenNewProject(prjName) { //select project and click Open
  try {
    //loop thru names in accordion
    var name, sel;
    var $nameCtls = $("#uxProjectsContainer span[id^='uxProjectName']");
    $nameCtls.each(function () {
      name = this.innerHTML;
      if (name.toLowerCase() !== prjName.toLowerCase()) return true; //next
      document.getElementById(this.id.replace("Name", "Select")).click();
      GetControlByTypeAndId("input", "uxOpenProject").click();
    });
  } catch (err) { HiUser(err, "Open New Project"); }
  SetWebServiceIndicators(false);
}
function GetProjectForWebService(action) {
  var json;
  try {
    var features = {};    // Create empty javascript object
    var $this, thisid, attr, attrType;
    $("#ux" + action + "ProjectMain :input").each(function () { // Iterate over inputs
      $this = $(this);
      thisid = $this.attr("id").replace("ux" + action + "Project", "");
      attr = $this.val();
      if (attr.trim().length == 0) attr = null;
      else {
        attrType = $this.attr("name");
        if ("number" === attrType) attr = parseFloat(attr);
        if ("date" === attrType) attr = (new Date(attr)).getTime(); // FormatDateForDatabaseInsert(new Date(attr)); //may need to format
      }
      if (typeof attr === 'string') attr = attr.replace("'", "\'");
      if (null !== attr) features[thisid] = attr;  // Add each to features object
    });
    if ("Edit" === action) { //add Guid
      features["OperationId"] = parseFloat(GetSelectedProjectOperationId());
    }
    json = JSON.stringify(features); // Stringify to create json object
  } catch (err) { HiUser(err, "Get Project For Web Service"); return null; }
  return json;
}
function DuplicateProject() {
  try {
    if (-1 === currRowSelect) { alert(noProjMsg); return false; }
    OpenForm("uxDuplicateProject");
  } catch (err) { HiUser(err, "Duplicate Project"); return null; }
}
function SubmitDuplicateProject(action) {
  try {
    var prjName = $("[id$=uxDuplicateProjectName]").val().trim();
    if (prjName.length <= 0) { HiUser("Please enter a new name"); return false; }
    if (!IsUniqueProjectName(prjName)) { HiUser("A project already exists with that name"); return false; }

    GetProjectIds();
    var projid = GetProjId();
    var svcData = {};
    svcData["projectId"] = parseInt(projid);
    svcData["name"] = prjName;
    svcData["notes"] = document.getElementById("ux" + action + "ProjectNotes").value;
    var infoMsg = "";
    var closeForm = true;

    SetWebServiceIndicators(true, "Duplicating Project");
    $.ajax({
      url: "GISTools.asmx/DuplicateProject"
    , data: JSON.stringify(svcData)
    })
    .done(function (msg) {
      projects = msg.d;
      if (projects.info && projects.info.length > 0) HiUser(projects.info, "Duplicate project succeeded");
      if (!projects || !projects.projects || projects.projects.length === 0) $("[id$=uxNoProjects]").html("You don't have any projects created. Use the button to create a new project.");
      else { $("[id$=uxNoProjects]").html(""); ShowProjects(); /*OpenNewProject(prjName);*/ }
    })
    .fail(function (jqXHR, textStatus, errorThrown) {
      closeForm = false;
      var errorResult = errorThrown;
      HiUser(errorResult, "Duplicate Project failed.");
    })
    .always(function () {
      FinishDuplicateProject(closeForm, action, infoMsg);
    });
    //    projectsRetrievedIndx = editingFeatIndx; //store project indx
  } catch (err) { HiUser(err, "Duplicate Project"); }
}
function FinishDuplicateProject(closeForm, action) {
  SetWebServiceIndicators(false);
  ClearProjectSelection();
  if (closeForm) CloseForm("ux" + action + "Project");

  var newId = GetNewProjectId();
  SelectProjectById(GetProjId());
}
function TransferProject() {
  try {
    //    HiUser("Not working yet."); return false;
    var count = 0;
    console.log(count++);
    if (-1 === currRowSelect) { alert(noProjMsg); return false; }
    var username = document.getElementById("ctl00_HeadLoginView_HeadLoginName").textContent;
    var email = document.getElementById("uxTransferProjectEmail");
    var projectname = GetProjectName();
    console.log(count++);
    document.getElementById("uxTransferProjectName").textContent = projectname;
    email.textContent = email.value.replace("{sender}", username).replace("{projectname}", projectname);
    console.log(count++);
    OpenForm("uxTransferProject");
    console.log(count++);
  } catch (err) { HiUser(err, "Transfer Project"); return null; }
}
function SubmitTransferProject(action) {
  try {
    var sendrName = document.getElementById("ctl00_HeadLoginView_HeadLoginName").textContent.trim();
    var usrName = document.getElementById("uxTransferProjectUserName").value.trim();
    if (usrName.length <= 0) { HiUser("Please enter a user to transfer the project to."); return false; }
    if (usrName.toLowerCase() === sendrName.toLowerCase()) {
      HiUser("Please enter a user name that is not yours. " + CR +
      CR + "If you wish to have a duplicate of your own project, use the Duplicate button."); return false;
    }

    var projid = GetProjId();
    var svcData = {};
    svcData["projectId"] = parseInt(projid);
    svcData["transfereeName"] = usrName;
    svcData["addlText"] = document.getElementById("uxTransferNotes").value;
    var closeForm = true;

    SetWebServiceIndicators(true, "Transferring Project");
    $.ajax({
      url: "GISTools.asmx/TransferProject"
    , data: JSON.stringify(svcData)
    })
    .done(function (msg) {
      var info = msg.d;
      if (info && info.length > 0) HiUser(info, "Transfer project service succeeded");
    })
    .fail(function (jqXHR, textStatus, errorThrown) {
      closeForm = false;
      var errorResult = errorThrown;
      HiUser(errorResult, "Transfer Project failed.");
    })
    .always(function () {
      FinishTransferProject(closeForm, action);
    });
  } catch (err) { HiUser(err, "Transfer Project"); }
}
function FinishTransferProject(closeForm, action) {
  SetWebServiceIndicators(false);
  if (closeForm) CloseForm("ux" + action + "Project");
}