/// <reference path="http://ajax.aspnetcdn.com/ajax/jQuery/jquery-1.5-vsdoc.js"/>
/// <reference path="Site.js" />

myRidgelines = {
  count: 0
  , cls: 'Ridgeline'
  , heading: 'Ridgeline'
  , color: "#7CFC00" // "#99FF33";
  , selColor: "#77F7E0" // "#8303FF" // "#6600CC";
  , features: {}
  , featureLabels: []
  , Init: function () {
    try { this.SetFeatures(); } catch (e) { HiUser(e, "Init Ridgelines"); }
  }
  , SetFeatures: function () {
    try {
      this.Hide();
      if (!ridgelinesJson || !(ridgelinesJson.ridgelines)) { this.features = {}; this.count = 0; return; }
      this.features = ridgelinesJson.ridgelines;
      this.count = this.features.length;
      this.Show();
    } catch (e) { HiUser(e, "Set Ridgelines"); }
  }
  , GetFeatureName: function (oid) {
    try {
      var feat = this.GetFeatureByOid(oid);
      var featRec = feat.ridgelineRecord;
      var name = featRec.RidgelineName;
      return name;
    } catch (e) { HiUser(e, "Get Ridgeline Name"); }
  }
  , GetFeatureByGuid: function (guid) {
    if (!guid || guid.length === 0) return null;
    guid = guid.toLowerCase();
    var feats = this.features;
    var featRec;
    try {
      for (var feat in feats) {
        featRec = feats[feat].datumRecord;
        if (featRec && guid == featRec.GUID.toLowerCase()) return feats[feat];
      }
    } catch (err) { HiUser(err, "Get Ridgeline By Guid"); }
    return null;
  }
  , GetFeatureByOid: function (oid) {
    if (!oid || oid.length === 0) return null;
    oid = ParseInt10(oid);
    var feats = this.features;
    var featRec;
    try {
      for (var feat in feats) {
        featRec = feats[feat].ridgelineRecord;
        if (featRec && oid == featRec.ObjectID) return feats[feat];
      }
    } catch (err) { HiUser(err, "Get Ridgeline By Oid"); }
    return null;
  }
  , GetFeatureByName: function (ridgelineName) {
    if (!ridgelineName) return null;
    var feats = this.features;
    var featRec, featName;
    try {
      for (var feat in feats) {
        featRec = feats[feat].ridgelineRecord;
        if (featRec) {
          featName = featRec.RidgelineName.toString().trim();
          if (featName == ridgelineName) return feats[feat];
        }
      }
    } catch (err) { HiUser(err, "Get Ridgeline By Name"); }
    return null;
  }
  , GetExtentByOids: function (oid) {
    var retVal = null;
    var haveAFeature = false;
    var newBounds = new GGLMAPS.LatLngBounds();
    var shapeBounds = null;
    var feats = this.features;
    var featRec;
    try {
      for (var feat in feats) {
        featRec = feats[feat].ridgelineRecord;
        if (featRec) {
          if (!oid || oid.indexOf(featRec.ObjectID) !== -1) {
            shapeBounds = GetBoundsForCoordsString(featRec.Coords);
            if (shapeBounds) {
              haveAFeature = true;
              newBounds.extend(shapeBounds.getSouthWest());
              newBounds.extend(shapeBounds.getNorthEast());
            }
          }
        }
      }
    } catch (err) { HiUser(err, "Get Ridgelines Extent"); }
    if (haveAFeature) { retVal = newBounds; }
    return retVal;
  }
  , GetInfoWindow: function (oid) {
    var html;
    try {
      var feat = this.GetFeatureByOid(oid);
      var featRec = feat.ridgelineRecord;
      html = "<div class='infoWin" + GetFeatureType(featureTypes.RIDGELINE) + "' id='" + GetFeatureType(featureTypes.RIDGELINE) + oid + "info'>";
      html += "<table class='" + this.cls.toLowerCase() + "Info' id='" + this.cls.toLowerCase() + oid + "'>";
      html += "<tr><th colspan='2' " +
        " style='background-color: " + this.selColor + ";' " +
        ">" + this.heading + "</th></tr>";

      var currData;
      currData = (featRec.Length * ftPerMtr).toFixed(1);
      html += "<tr><td class='first'>" + "Length (ft)" + ": </td>";
      html += "<td>" + unescape(currData);
      html += "</td></tr>";

      html += "</table></div>";

    } catch (err) { HiUser(err, "Get Ridgeline Info Window"); }
    return { content: html, position: feat.geometry.center };
  }
  , HighlightFeature: function (oid) {
    try {
      var feats = this.features;
      var featRec, featGeom;
      try {
        for (var featx in feats) {
          var feat = feats[featx];
          featRec = feat.ridgelineRecord, featGeom = feat.geometry;
          if (!featRec) continue;
          var featOid = featRec.ObjectID;
          if (featOid.toString() != oid.toString() && featGeom.strokeColor.toLowerCase() === ridgelineStrokeHighlight.toLowerCase()) featGeom.setOptions({ strokeColor: ridgelineStrokeColor });
        }
      } catch (err) { HiUser(err, "Dehighlight Ridgeline"); }
      var feat0 = this.GetFeatureByOid(oid);
      featGeom = feat0.geometry;
      featGeom.setOptions({ strokeColor: ridgelineStrokeHighlight });
    } catch (err) { HiUser(err, "Highlight Ridgeline"); }
  }
  , RemoveHighlights: function () {
    var feats = this.features;
    var featGeom;
    try {
      for (var feat in feats) {
        featGeom = feats[feat].geometry;
        if (featGeom) featGeom.setOptions({ strokeColor: ridgelineStrokeColor });
      }
    } catch (err) { HiUser(err, "Remove Ridgeline Highlights"); }
  }
  , Hide: function (oid) {
    try {
      if (oid) { this.GetFeatureByOid(oid).Hide(); }
      else {
        var feats = this.features;
        for (var feat in feats) if (feats[feat] && typeof feats[feat].Hide === 'function') feats[feat].Hide();
      }
    } catch (err) { HiUser(err, "Hide Ridgelines"); }
  }
  , Show: function (oid) {
    try {
      if (oid) { this.GetFeatureByOid(oid).Show(); }
      else {
        var feats = this.features;
        for (var feat in feats) if (feats[feat] && typeof feats[feat].Show === 'function') feats[feat].Show();
      }
    } catch (err) { HiUser(err, "Show Ridgelines"); }
  }
  , GetLabel: function (oid) {
    var retVal = "not found";
    try {
      var feat = this.GetFeatureByOid(oid);
      var featRec = feat.ridgelineRecord;
      var name;
      name = featRec.RidgelineName;
      retVal = name.toString();
    } catch (err) { HiUser(err, "Get Ridgeline Label"); }
    return retVal;
  }
  , ToggleLabel: function (sendr) {
    try {
      if (sendr.checked) this.ShowLabel();
      else this.HideLabel();
    } catch (err) { HiUser(err, "Toggle Ridgeline Label"); }
  }
  , HideLabel: function (sendr) {
    try {
      var lblsLen = this.featureLabels.length;
      for (var lblIdx = 0; lblIdx < lblsLen; lblIdx++) { this.featureLabels[lblIdx].hide(); }
    } catch (err) { HiUser(err, "Hide Ridgeline Labels"); }
  }
  , ShowLabel: function (oid) {
    var lblsLen = this.featureLabels.length;
    for (var lblIdx = 0; lblIdx < lblsLen; lblIdx++) { this.featureLabels[lblIdx].hide(); }
    this.featureLabels = [];
    var feats = this.features;
    var labelText, txtWid;
    var myOptions;
    var ibLabel;
    var featRec;
    var name;
    try {
      for (var feat in feats) {
        featRec = feats[feat].ridgelineRecord;
        if (featRec) {
          labelPos = feats[feat].geometry.center;
          if (!labelPos) continue;
          name = "ID: " + featRec.RidgelineName;
          name += "<br />" + "Acres: " + featRec.TotalArea;
          name += "<br />" + "Spr.: " + featRec.SpreadableArea;
          labelText = name;
          myOptions = {
            content: labelText
          , boxStyle: {
            border: "0px solid black"
            , textAlign: "left"
            , padding: ".1em"
            , fontSize: "8pt"
            , color: "blue"
            , background: "white"
            , opacity: 0.75
          }
          , disableAutoPan: true
          , pixelOffset: new GGLMAPS.Size(-30, 0)
          , position: labelPos
          , closeBoxURL: ""
          , isHidden: false
          , pane: "floatPane"
          , enableEventPropagation: true
          };
          ibLabel = new InfoBox(myOptions);
          ibLabel.open(gglmap);
          this.featureLabels.push(ibLabel);
        }
      }
    } catch (e) { HiUser(e, "Show Ridgeline Labels"); }
  }
}

var ridgelinesJson, ridgelinesJsonD;
var ridgelineStrokeColor = myRidgelines.color;
var ridgelineStrokeWeight = 4;
var ridgelineStrokeOpacity = 1.0;
var ridgelineZIndex = 9;
var ridgelineStrokeHighlight = myRidgelines.selColor;

var cancelRidgelineDrawHandled = false;

var ridgelineStyles = [];

function RidgelineStyle() {
  this.name = "Ridgeline";
  this.color = ridgelineStrokeColor;
  this.width = ridgelineStrokeWeight;
  this.lineopac = ridgelineStrokeOpacity;
  this.zindex = ridgelineZIndex;
}
function CreateRidgelineStyleObject() {
  var linestyle = new RidgelineStyle(); ridgelineStyles.push(linestyle);
  var tmpStrokeColor = ridgelineStrokeColor;
  ridgelineStrokeColor = ridgelineStrokeHighlight; linestyle = new RidgelineStyle(); ridgelineStyles.push(linestyle);
  ridgelineStrokeColor = tmpStrokeColor;
}
function PrepareRidgeline(styleArray, styleIndx) {
  try {
    if (!styleArray) styleArray = ridgelineStyles;
    if (!styleIndx) styleIndx = 0;
    //console.log("PrepareRidgeline polyPoints", polyPoints);
    var polyOptions = {
      path: polyPoints
    , strokeColor: styleArray[styleIndx].color
    , strokeOpacity: styleArray[styleIndx].lineopac
    , strokeWeight: styleArray[styleIndx].width
    , zIndex: styleArray[styleIndx].zindex
    };
    polyShape = new GGLMAPS.Polyline(polyOptions);
    polyShape.setMap(gglmap);
  } catch (e) { HiUser(e, "Prepare Ridgeline"); }
  try {
    //if (!styleArray) styleArray = ridgelineStyles;
    //if (!styleIndx) styleIndx = 0;
    //console.log("PrepareRidgeline polyPoints", polyPoints);
    //var polyOptions = {
    //  path: polyPoints
    //, strokeColor: styleArray[styleIndx].color
    //, strokeOpacity: 0.01
    //, strokeWeight: 25
    //, zIndex: styleArray[styleIndx].zindex - 1
    //};
    //polyShape = new GGLMAPS.Polyline(polyOptions);
    //polyShape.setMap(gglmap);
  } catch (e) { HiUser(e, "Prepare Ridgeline Selector"); }
}

function InitializeRidgelines() {
  CreateRidgelineStyleObject();
  if (ridgelinesJson) {
    ridgelinesJsonD = ridgelinesJson.d; //set as if web service call
    LoadRidgelinesDone();
    myRidgelines.Init();
  }
}

function ClearRidgelineSelection(params) {
  try {
    myRidgelines.RemoveHighlights();
    ClearEditFeat();
  } catch (e) { HiUser(e, "Clear Ridgeline Selection"); }
}
var ridgelineMapOrTable; //track where selection was made
var selectedRidgelineId;
function SelectRidgelineInMap(oid) { try { FeatureClickFunction(featureTypes.RIDGELINE, oid); } catch (e) { HiUser(e, "Select Ridgeline In Map"); } }
function SelectRidgelineInTable(oid) {
  try {
    var sels = $("[id*='uxRidgelineOid']");
    var ids = "", $this, thisid, sendrId = "";
    sels.each(function () { // Iterate over items
      $this = $(this);
      thisid = $this.attr("id");
      if ($this.val() == oid) sendrId = thisid.replace("Oid", "Select");
    });
    if (sendrId !== "") {
      var sendr = GetControlByTypeAndId("input", sendrId);
      ProcessSelectRidgeline(sendr);
    }
  } catch (e) { HiUser(e, "Select Ridgeline In Table"); }
}
function ProcessSelectRidgeline(sendr) {
  try {
    var sendrId = sendr.id;
    //turn off other selections, reset css
    $('#' + sendrId).prop("checked", true);
    $('#uxRidgelineContainer input[type="radio"]:not(#' + sendrId + ')').prop("checked", false);
    $('#uxRidgelineContainer input[type="radio"]:not(#' + sendrId + ')').parent().removeClass("accord-header-highlight");
    //highlight selection
    $(sendr).parent().addClass("accord-header-highlight");

    var oid = $("#" + sendr.id.replace("Select", "Oid")).val();
    var feat = myRidgelines.GetFeatureByOid(oid);

    if ("table" === ridgelineMapOrTable) { GGLMAPS.event.trigger(feat.geometry, 'click', {}); }
    //    if ("table" === ridgelineMapOrTable) FeatureClickListener(feat, featureTypes[0], featureGeometrys[2], oid, google.maps.event.trigger(feat, 'click'));

    selectedRidgelineId = sendrId;
    EnableTools('ridgeline');
    $.observable(ridgelinesJson).setProperty("selectedID", GetSelectedRidgelineId());
  } catch (e) { HiUser(e, "Process Select Ridgeline"); }
}

function StartDrawingRidgeline(sendr) {
  var okToCont = false;
  try {
    var poly0 = new GGLMAPS.MVCArray();
    polyPoints = new GGLMAPS.MVCArray();
    var viewBnds = gglmap.getBounds();
    var featBnds = null;
    if (actionType === actionTypes.EDIT) {
      okToCont = true;
      preEditCoords = editFeat.ridgelineRecord.Coords;
      polyShape = editFeat.geometry;
      if (polyShape) {
        var pth, pthLen;
        pth = polyShape.getPath();
        pthLen = pth.getLength();
        poly0 = new GGLMAPS.MVCArray();
        for (i = 0; i < pthLen; i++) {
          if (isNaN(pth.getAt(i).lat()) || isNaN(pth.getAt(i).lng())) continue;
          poly0.push(pth.getAt(i));
        }
        polyPoints.push(poly0);
        polyPoints = poly0;
      }
      PrepareRidgeline(ridgelineStyles, 1);
      polyShape.setEditable(true);
      AddFeatureClickEvents(polyShape, featureType, featureGeometry, editingOid);
      editFeat.geometry.setMap(null);
      featBnds = myRidgelines.GetExtentByOids([editingOid]);
      if (featBnds && viewBnds && !viewBnds.intersects(featBnds) && polyShape && polyShape.getPaths()) { SetMapExtentByOids(featureTypes.RIDGELINE, [editingOid]); }
    } else { //not editing 
      featureInfoOk = 1; // VerifyRidgelineInfo();
      if (featureInfoOk === 1) {
        okToCont = true;
        featureGeometry = featureGeometrys[1];
        RemoveClass(document.getElementById("uxCreateRidgelineDrawSubmit"), "display-none");
        polyPoints = new GGLMAPS.MVCArray(); //only 1 path for lines
        PrepareRidgeline(ridgelineStyles, 0);
        polyShape.setEditable(true);
      } else {
        okToCont = false;
      } // END: featureInfoOk
    } // END: editing
    ShowToolsMainDiv(true);
  } catch (e) { HiUser(e, "Start Drawing"); }
  return okToCont;
}

function SelectRidgeline(sendr, ev) {
  try {
    if (actionType) return;
    ClearTableSelections(featureTypes.RIDGELINE);
    ridgelineMapOrTable = "table"; //selected from table, run map selection
    infowindow.close();
    infowindow = new GGLMAPS.InfoWindow();
    //validate new selection
    var isChecked = sendr.checked;
    if (isChecked && sendr.id === selectedRidgelineId) return; //no change

    ProcessSelectRidgeline(sendr);
    //stopPropagation or else radio button becomes unselected
    ev.stopPropagation();
  } catch (e) { HiUser(e, "Select Ridgeline"); }
}
function GetSelectedRidgelineId() {
  var retVal = "";
  try {
    if (-1 == selectedRidgelineId) return "";
    var idCtlName = selectedRidgelineId.replace("Select", "Guid");
    var idCtl = $("#" + idCtlName + "");
    if (1 > idCtl.length) return ""; //not found
    retVal = idCtl.val();
  } catch (e) { HiUser(e, "Get Selected Ridgeline Id"); }
  return retVal;
}
function GetRidgelineNameFromUserInput(action) {
  var ridgelineId = GetControlByTypeAndId("input", "ux" + action + "RidgelineRidgelineName").value.trim();
  return ridgelineId;
}
function PreSelectRidgeline() {
  $("#uxRidgelineSelect0").click();
}
function EditRidgeline(sendr) {
  try {
    if (myRidgelines.count > 0) {
      PreSelectRidgeline();
      window.setTimeout(EditRidgeline_Part2, 250, sendr);
    } else {
      HiUser("No ridgeline exists.\n\nPlease create a ridgeline first.");
      return;
    }
  } catch (err) { HiUser(err, "Edit Ridgeline"); }
}
function EditRidgeline_Part2(sendr) {
  try {
    if (!editFeat || featureTypes.RIDGELINE !== editingFeatType) { alert("Please select a ridgeline first."); return false; }
    infowindow.close();
    LoadRidgelineOptions("Edit");
    EditFeature(sendr, featureTypes.RIDGELINE, editingFeatIndx, editingOid);
  } catch (err) { HiUser(err, "Edit Ridgeline 2"); }
}
function BeginNewRidgeline(sendr) {
  try {
    if (myRidgelines.count > 0) {
      alert("Only one ridgeline is allowed. Please edit or delete the current ridgeline if you want to change it.");
      return;
    }
    BeginNewFeature(sendr);
    featureType = featureTypes.RIDGELINE;
    ClearRidgelineToolsForm();
    ShowFeatureTools(featureType, sendr);
    $("#uxRidgelineDrawStart").click();
  } catch (e) { HiUser(e, "Begin New Ridgeline"); }
}
function LoadRidgelineOptions(action) {}
function FinishSubmitRidgeline(closeForm, action) {
  FinishSubmitFeature(closeForm, action);
  ClearRidgelineSelection();
  if (closeForm) CloseForm("ux" + action + "Ridgeline");
  inDrawMode = false;
}

function DeleteRidgeline(oid) {
  try {
    if (myRidgelines.count > 0) {
      oid = myRidgelines.features[0].ridgelineRecord.ObjectID;
      PreSelectRidgeline();
      window.setTimeout(DeleteRidgeline_Part2, 250, oid);
    } else {
      HiUser("No ridgeline exists to delete.");
      return;
    }
  } catch (e) { HiUser(e, "Delete Ridgeline"); }
}
function DeleteRidgeline_Part2(oid) {
  try {
    infowindow.close();
    if ("undefined" === typeof editingFeatType || featureTypes.RIDGELINE !== editingFeatType
      /*|| "undefined" === typeof editingFeatIndx || 0 > editingFeatIndx*/ || "undefined" === typeof editingOid || 0 > editingOid) { alert("Please select a ridgeline first"); return false; }
    if ("undefined" === typeof oid || oid < 0) { alert("Please select a ridgeline first."); return false; }
    var ovToDel = myRidgelines.GetFeatureByOid(oid);

    var confMsg = CR + CR;
    confMsg += "Are you sure you want to delete this ridgeline?" + CR + CR + CR;
    var YorN = confirm(confMsg);
    if (YorN) {
      try {
        $("[id$=uxRidgelinesInfo]").html("");
        var projId = GetProjId();
        var svcData = "{projectId:{1},id:'{2}'}".replace("{1}", ParseInt10(projId)).replace("{2}", oid);
        SetWebServiceIndicators(true, "Deleting Ridgeline");
        $.ajax({
          url: "GISTools.asmx/DeleteRidgeline"
          , data: svcData
        })
        .done(function (data, textStatus, jqXHR) {
          ovToDel.Hide();
          ridgelinesJsonD = data.d;
          if (ridgelinesJsonD.info && ridgelinesJsonD.info.length > 0) HiUser(ridgelinesJsonD.info, "Delete Ridgeline succeeded");
          LoadRidgelinesDone();
          myRidgelines.SetFeatures();
          infowindow.close();
        })
        .fail(function (jqXHR, textStatus, errorThrown) {
          var errorResult = errorThrown;
          HiUser(errorResult, "Delete Ridgeline failed.");
        })
        .always(function () {
          SetWebServiceIndicators(false);
          ClearRidgelineSelection();
        });
        //    ridgelinesRetrievedIndx = editingFeatIndx; //store ridgeline indx for reselection
      } catch (err) { HiUser(err, "Delete Ridgelines 2"); SetWebServiceIndicators(false); }
    }
  } catch (e) { HiUser(e, "Delete Ridgeline 2"); }
}
function ShowRidgelineTools(feattype, sendr) {
  try {
    var sendrId = sendr.id;
    if (sendrId.indexOf("Edit") > -1) {
      var feat = myRidgelines.GetFeatureByOid(editingOid);
      var add = document.getElementById("uxEditRidgelineDrawNew");
      var mve = document.getElementById("uxEditRidgelineDrawStart");
      if (feat.ridgelineRecord.Shape == "") { AddClass(mve, "display-none"); RemoveClass(add, "display-none"); }
      else { AddClass(add, "display-none"); RemoveClass(mve, "display-none"); }
    }

    inDrawMode = false;
    SetDisplayWithToolsOpen(false);
    var featdesc = "Ridgeline";

    var toolsObj = GetControlByTypeAndId("div", "uxCreateRidgelineContainer");
    if (actionType === actionTypes.EDIT) toolsObj = GetControlByTypeAndId("div", "uxEditRidgelineContainer");
     
    SetDisplayCss(toolsObj, true); // show tools div
    ShowToolsMainDiv(true); // show options part of div
    if (actionType === actionTypes.ADD) ClearToolsFormsOptions(); // clear things if adding new
    featureGeometry = featureGeometrys[1];

    //SetFormBaseLocation(toolsObj,"uxRidgelinesContainer"); 
    SetStartDrawingButtonText("Start Drawing", "Start drawing a new " + featdesc.toLowerCase());
    SetSubmitButtonText("Submit", "Submit the new " + featdesc.toLowerCase());

    if (actionType === actionTypes.EDIT) {
      SetStartDrawingButtonText("Start Drawing", "Draw a shape for the current feature");
      switch (feattype) {
        case featureTypes.RIDGELINE:
          if (editFeat.ridgelineRecord.Coords.trim().length > 0) SetStartDrawingButtonText("Edit Shape", "Edit the shape of the current feature");
          OpenForm('uxEditRidgeline');
          break;
        default:
          HiUser("Feature type is not set.", "Show " + featdesc + " Tools");
          break;
      } // END: feattype check (switch)

      SetSubmitButtonVis(true);
      SetSubmitButtonText("Submit Edits", "Submit edits to database");
    }

  } catch (e) { HiUser(e, "Show Ridgeline Tools"); }
}
function CancelRidgelineDraw(param) {
  try {
    if (actionType === actionTypes.EDIT && inDrawMode) {
      if (polyShape) polyShape.setMap(null);
      polyPoints = new GGLMAPS.MVCArray();
      if (editFeat) { SetRidgelineGeometry(editFeat, preEditCoords, editingOid); myRidgelines.HighlightFeature(editingOid); }
      gglmap.setOptions({ disableDoubleClickZoom: false, draggableCursor: 'auto' });
      document.body.style.cursor = 'auto';
      cancelRidgelineDrawHandled = true;
    } else if (actionType === actionTypes.EDIT) {
      if (cancelRidgelineDrawHandled !== true) CancelDraw();
      cancelRidgelineDrawHandled = true;
    } else {
      CancelDraw();
    }

    if (HasClass(GetControlByTypeAndId('div', 'uxCreateRidgelineMain'), "display-none")) ShowToolsMainDiv(true);
    else HideRidgelineTools();

    inDrawMode = false;
    SetDisplayStartDrawingButtons(true);
    if (param === "submitted") {
      SetVisibilityCss(GetControlByTypeAndId('input', 'uxRidgelineAddNew'), true);
      SetStartDrawingButtonText("Edit Shape", "Edit the shape of the current feature");
      EditFeature(null, featureTypes.HIGHPOINT, editingFeatIndx, editingOid);
    }
    if (polyShape) polyShape.setMap(null);
    polyShape = null;
  } catch (e) { HiUser(e, "Cancel Ridgeline Draw"); }
}
function HideRidgelineTools() {
  SetDisplayCss(GetControlByTypeAndId("div", "uxCreateRidgelineContainer"), false);
  CloseForm('uxEditRidgeline');
  HideFeatureTools();
}
function ClearRidgelineToolsForm() {
  try {
  } catch (e) { HiUser(e, "Clear Ridgeline Tools Form"); }
}
function GetRidgelineForWebService(action) {
  var features = {}; 
  return features; //nothing needed right now

  var json, strCount = 0, datatypes = "";
  try {
    var features = {};    // Create empty javascript object
    var $this, thisid, attr, dataType, dte, replcVal = "ux" + action + "Ridgeline";
    $("#" + replcVal + "Main").find("[id^=" + replcVal + "]").filter(":input").each(function () { // Iterate over inputs
      try {
        $this = $(this);
        thisid = $this.attr("id");
        if (!thisid || thisid.indexOf(replcVal) < 0) return true; // == continue
        thisid = thisid.replace(replcVal, "");
        dataType = $this.attr("data-type");
        if (datatypes.indexOf(dataType) < 0) datatypes += " " + dataType;
        if ("check" === dataType) attr = $this.prop("checked") === true ? 1 : null;
        else {
          attr = $this.val().trim();
          if (attr.trim().length == 0) attr = null;
          if (null != attr) {
            if ("number" === dataType) {
              attr = isNaN(parseFloat(attr)) ? null : parseFloat(attr);
              if (this.tagName.toLowerCase() === "select" && -1 == attr) attr = null;
            }
            if ("date" === dataType) {
              dte = new Date(attr);
              attr = IsValidDate(dte) ? dte : null;
            }
          }
        }
        if (typeof attr === 'string') { strCount += 1; attr = attr.replace("'", "\'"); }
        if (null !== attr) features[thisid] = attr;  // Add each to features object
      } catch (e) { HiUser(e, "Get value for " + thisid); }
    });
    if ("Edit" === action) {
    }
    if (!features["RidgelineName"]) features["RidgelineName"] = GetProjectName();
    json = JSON.stringify(features); // Stringify to create json object
  } catch (e) { HiUser(e, "Get Ridgeline For Web Service"); return null; }
  return features;
}
function ReloadRidgelines() {
  try {
    $("[id$=uxRidgelinesInfo]").html("");
    SetWebServiceIndicators(true, "Getting ridgelines");
    var projId = GetProjId();
    var svcData = "{projectId:{0}}".replace("{0}", ParseInt10(projId));
    $.ajax({
      url: "GISTools.asmx/GetRidgelines"
      , data: svcData
    })
    .done(function (data, textStatus, jqXHR) {
      ridgelinesJsonD = data.d;
      if (ridgelinesJsonD.info && ridgelinesJsonD.info.length > 0) HiUser(ridgelinesJsonD.info, "Get Ridgelines succeeded");
      LoadRidgelinesDone();
      myRidgelines.SetFeatures();
    })
    .fail(function (jqXHR, textStatus, errorThrown) {
      var errorResult = errorThrown;
      HiUser(errorResult, "Get Ridgelines failed.");
    })
    .always(function () {
      SetWebServiceIndicators(false);
      ClearRidgelineSelection();
    });
    //    ridgelinesRetrievedIndx = editingFeatIndx; //store ridgeline indx for reselection
  } catch (err) { HiUser(err, "Load Ridgelines"); SetWebServiceIndicators(false); }
}
function LoadRidgelinesDone() {
  try {
    var info = "";
    if (!ridgelinesJsonD || !ridgelinesJsonD.ridgelines || ridgelinesJsonD.ridgelines.length === 0) info = "You do not have any ridgelines created. Use the tools button to create a new ridgeline.";

    RenderRidgelines();
    $("[id$=uxRidgelinesInfo]").html(info); //set after linking or DNE
  } catch (e) { HiUser(e, "Load Ridgelines Done"); }
}
function RenderRidgelines() {
  try {
    if (!ridgelinesJsonD || !ridgelinesJsonD.ridgelines || ridgelinesJsonD.ridgelines.length === 0) {
      ridgelinesJson = {};
      return;
    }
    var ridgelinesJsonRidgelines = ridgelinesJsonD.ridgelines;

    ridgelinesJson = {
      ridgelines: ridgelinesJsonRidgelines
      , selectedID: (ridgelinesJsonRidgelines && ridgelinesJsonRidgelines.length > 0) ? ridgelinesJsonRidgelines[0].datumRecord.GUID : '0'
      , selected: function () {
        try {
          for (var i = 0; i < ridgelinesJsonRidgelines.length; i++) {
            if (ridgelinesJsonRidgelines[i].datumRecord.GUID === this.selectedID) {
              return ridgelinesJsonRidgelines[i];
            }
          }
        } catch (e) { HiUser(e, "Show Ridgelines selected"); }
        return {};
      }
    };
    FleshOutRidgelines();

    ridgelinesJson.selected.depends = "selectedID";

    ridgelinesTmpl.link("#uxRidgelineContainer", ridgelinesJson);
    editRidgelinesTmpl.link("#uxEditRidgelineContainer", ridgelinesJson);
    SetAccordions();
    $('input:radio[name*="RidgelineSelect"]').off('click').on('click', function (e) { SelectRidgeline(this, e); });
  } catch (e) { HiUser(e, "Render Ridgelines"); }
}
function FleshOutRidgelines() {
  try {
    var feats = ridgelinesJson.ridgelines;
    var feat, featRec;
    for (var f in feats) {
      feat = feats[f];
      featRec = feat.ridgelineRecord;
      if (!featRec) continue;
      feat.geometry = new GGLMAPS.Polyline();
      SetRidgelineGeometry(feat, featRec.Coords, featRec.ObjectID);
      feat.Show = function () { this.geometry.setMap(gglmap); };
      feat.Hide = function () { this.geometry.setMap(null); };
    }
  } catch (e) { HiUser(e, "Flesh Out Ridgelines"); }
}
function SetRidgelineGeometry(feat, featCoords, featOid) {
  try {
    polyPoints = CreateMvcPointArray(featCoords);
    PrepareRidgeline(ridgelineStyles, 0);
    feat.geometry = polyShape;
    feat.geometry.parent = featOid;
    feat.geometry.bounds = GetBoundsForPoly(polyShape);
    feat.geometry.center = GetCenterOfCoordsString(featCoords);
    AddFeatureClickEvents(feat.geometry, featureTypes.RIDGELINE, featureGeometrys[2], featOid);
    ClearDrawingEntities();
  } catch (e) { HiUser(e, "Set Ridgeline Geometry"); }
}
