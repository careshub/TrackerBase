/// <reference path="http://ajax.aspnetcdn.com/ajax/jQuery/jquery-1.5-vsdoc.js"/>
/// <reference path="Site.js" />

var myHighPoints = {
  count: 0
  , cls: "highPoint"
  , heading: 'High Point'
  , color: "#0000FF"
  , selColor: "#FFFF00"
  , features: {}
  , highPointLabels: []
  , Init: function () {
    try { this.SetFeatures(); } catch (e) { HiUser(e, "Init High Points"); }
  }
  , SetFeatures: function () {
    try {
      this.Hide();
      if (!highPointsJson || !(highPointsJson.highPoints)) { this.features = {}; this.count = 0; return; }
      this.features = highPointsJson.highPoints;
      this.count = this.features.length;
      this.Show();
    } catch (e) { HiUser(e, "Set High Points"); }
  }
  , GetFeatureName: function (oid) {
    if (!oid || oid.length === 0) return null;
    oid = ParseInt10(oid, 10);
    var feats = this.features;
    var featRec;
    try {
      for (var feat in feats) {
        featRec = feats[feat].highPointRecord;
        if (featRec && oid == featRec.ObjectID) return feats[feat].highPointRecord.HighPointName;
      }
    } catch (e) { HiUser(e, "Get High Point Name"); }
    return null;
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
    } catch (err) { HiUser(err, "Get " + this.heading + " By Guid"); }
    return null;
  }
  , GetFeatureByOid: function (oid) {
    if (!oid || oid.length === 0) return null;
    oid = ParseInt10(oid, 10);
    var feats = this.features;
    var featRec;
    try {
      for (var feat in feats) {
        featRec = feats[feat].highPointRecord;
        if (featRec && oid == featRec.ObjectID) return feats[feat];
      }
    } catch (e) { HiUser(e, "Get " + this.heading + " By Oid"); }
    return null;
  }
  , GetFeatureByName: function (highPointName) {
    if (!highPointName) return null;
    highPointName = highPointName.trim();
    var feats = this.features;
    var featRec, featName;
    try {
      for (var feat in feats) {
        featRec = feats[feat].highPointRecord;
        if (featRec) {
          featName = featRec.HighPointName.trim();
          if (featName == highPointName) return feats[feat];
        }
      }
    } catch (e) { HiUser(e, "Get High Point By Name"); }
    return null;
  }
  , GetExtentByOids: function (oids) {
    var retVal = null;
    var haveAFeature = false;
    var newBounds = new GGLMAPS.LatLngBounds();
    var shapeBounds = null;
    var feats = this.features;
    var featRec;
    try {
      for (var feat in feats) {
        featRec = feats[feat].highPointRecord;
        if (featRec) {
          if (!oids || oids.indexOf(featRec.ObjectID) !== -1) {
            shapeBounds = GetBoundsForCoordsString(GetHighPointCoords(featRec));
            if (shapeBounds) {
              haveAFeature = true;
              newBounds.extend(shapeBounds.getSouthWest());
              newBounds.extend(shapeBounds.getNorthEast());
            }
          }
        }
      }
    } catch (e) { HiUser(e, "Get High Points Extent"); }
    if (haveAFeature) retVal = newBounds;
    return retVal;
  }
  , GetInfoWindow: function (oid) {
    var html;
    try {
      var feat = this.GetFeatureByOid(oid);
      var featRec = feat.highPointRecord;
      html = "<div class='infoWin" + GetFeatureType(featureTypes.HIGHPOINT) + "' id='" + GetFeatureType(featureTypes.HIGHPOINT) + oid + "info'>";
      html += "<table class='" + this.cls + "Info' id='" + this.cls + oid + "'>";
      html += "<tr><th colspan='2' " +
        " style='background-color: " + this.selColor + ";' " +
        ">" + this.heading + "</th></tr>";

      var currData;
      currData = featRec.Latitude.toFixed(6);
      html += "<tr><td class='first'>" + "Lat" + ":</td>";
      html += "<td>" + unescape(currData);
      html += "</td></tr>";
      currData = featRec.Longitude.toFixed(6);
      html += "<tr><td class='first'>" + "Lng" + ":</td>";
      html += "<td>" + unescape(currData);
      html += "</td></tr>";

      html += "</table></div>";

    } catch (e) { HiUser(e, "Get High Point Info Window"); }
    return { content: html, position: feat.geometry.center };
  }
  , HighlightFeature: function (oid) {
    try {
      var feats = this.features;
      var featRec, featGeom;
      try {
        for (var featx in feats) {
          var feat = feats[featx];
          featRec = feat.highPointRecord, featGeom = feat.geometry;
          if (!featRec) continue;
          var featOid = featRec.ObjectID;
          if (featOid.toString() != oid.toString() && featGeom.getIcon().toLowerCase() === highPointIconHighlight.toLowerCase()) featGeom.setOptions({ icon: highPointStyles[0].icon });
        }
      } catch (e) { HiUser(e, "Dehighlight High Point"); }
      var feat0 = this.GetFeatureByOid(oid);
      featGeom = feat0.geometry;
      featGeom.setOptions({ icon: highPointStyles[1].icon });

    } catch (e) { HiUser(e, "Highlight High Point"); }
  }
  , RemoveHighlights: function () {
    var feats = this.features;
    var featGeom;
    try {
      for (var feat in feats) {
        featGeom = feats[feat].geometry;
        if (featGeom) featGeom.setOptions({ icon: highPointStyles[0].icon });
      }
    } catch (e) { HiUser(e, "Remove High Point Highlights"); }
  }
  , Hide: function (oid) {
    try {
      if (oid) { this.GetFeatureByOid(oid).Hide(); }
      else {
        var feats = this.features;
        for (var feat in feats) if (feats[feat] && typeof feats[feat].Hide === 'function') feats[feat].Hide();
      }
    } catch (e) { HiUser(e, "Hide High Points"); }
  }
  , Show: function (oid) {
    try {
      if (oid) { this.GetFeatureByOid(oid).Show(); }
      else {
        var feats = this.features;
        for (var feat in feats) if (feats[feat] && typeof feats[feat].Show === 'function') feats[feat].Show();
      }
    } catch (e) { HiUser(e, "Show High Points"); }
  }
  , GetLabel: function (oid) {
    var retVal = "not found";
    try {
      var feat = this.GetFeatureByOid(oid);
      var featRec = feat.highPointRecord;
      var name;
      name = featRec.HighPointName;
      retVal = name.toString();
    } catch (e) { HiUser(e, "Get High Point Label"); }
    return retVal;
  }
  , ShowLabel: function (oid) {
    var lblsLen = this.highPointLabels.length;
    for (var lblIdx = 0; lblIdx < lblsLen; lblIdx++) { this.highPointLabels[lblIdx].hide(); }
    this.highPointLabels = [];
    var feats = this.features;
    var labelText, txtWid;
    var myOptions;
    var ibLabel;
    var featRec;
    var name;
    try {
      for (var feat in feats) {
        featRec = feats[feat].highPointRecord;
        if (featRec) {
          labelPos = feats[feat].geometry.center;
          if (!labelPos) continue;
          name = featRec.HighPointName;
          labelText = name.toString();
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
          this.highPointLabels.push(ibLabel);
        }
      }
    } catch (e) { HiUser(e, "Label High Points"); }
  }
}

var highPointsJson, highPointsJsonD;
var highPointFeatZIndex = 11; 
var cancelHighPointDrawHandled = false;
var highPointIconImage = "/images/BlueStar.png";
var highPointIconHighlight = "/images/YellowStar.png";
var highPointStyles = []; 
var highPointPoint;
var highPointMapOrTable; //track where selection was made
var selectedHighPointId;
function HighPointStyle() {
  this.name = "High Point";
  this.icon = highPointPoint;
  this.zindex = highPointFeatZIndex;
}
function CreateHighPointStyleObject() {
  highPointPoint = highPointIconImage;
  var thismarker = new HighPointStyle(); highPointStyles.push(thismarker);
  highPointPoint = highPointIconHighlight;
  thismarker = new HighPointStyle(); highPointStyles.push(thismarker);
}
function PrepareHighPoint(styleArray, styleIndx) {
  if (!styleArray) styleArray = highPointStyles;
  if (!styleIndx) styleIndx = 0;
  var pointOptions = {
    position: polyPoints.getAt(0).getAt(0)
    , icon: styleArray[styleIndx].icon
    , zIndex: styleArray[styleIndx].zindex
  };
  polyShape = new GGLMAPS.Marker(pointOptions);
  polyShape.setMap(gglmap);
}
function InitializeHighPoints() {
  CreateHighPointStyleObject();
  if (highPointsJson) {
    highPointsJsonD = highPointsJson.d; //set as if web service call
    LoadHighPointsDone();
    myHighPoints.Init();
  }
}
function ClearHighPointSelection(params) {
  try {
    myHighPoints.RemoveHighlights();
    ClearEditFeat();
  } catch (e) { HiUser(e, "Clear High Point Selection"); }
}
function SelectHighPointInMap(oid) { try { FeatureClickFunction(featureTypes.HIGHPOINT, oid); } catch (e) { HiUser(e, "Select High Point In Map"); } }
function SelectHighPointInTable(oid) {
  try {
    var sels = $("[id*='uxHighPointOid']");
    var ids = "", $this, thisid, sendrId = "";
    sels.each(function () { // Iterate over items
      $this = $(this);
      thisid = $this.attr("id");
      if ($this.val() == oid) sendrId = thisid.replace("Oid", "Select");
    });
    if (sendrId !== "") {
      var sendr = GetControlByTypeAndId("input", sendrId);
      ProcessSelectHighPoint(sendr);
    }
  } catch (e) { HiUser(e, "Select High Point In Table"); }
}
function ProcessSelectHighPoint(sendr) {
  try {
    var sendrId = sendr.id;
    //turn off other selections, reset css
    $('#' + sendrId).prop("checked", true);
    $('#uxHighPointsContainer input[type="radio"]:not(#' + sendrId + ')').prop("checked", false);
    $('#uxHighPointsContainer input[type="radio"]:not(#' + sendrId + ')').parent().removeClass("accord-header-highlight");
    //highlight selection
    $(sendr).parent().addClass("accord-header-highlight");

    var oid = $("#" + sendr.id.replace("Select", "Oid")).val();
    var feat = myHighPoints.GetFeatureByOid(oid);
    if ("table" === highPointMapOrTable) { GGLMAPS.event.trigger(feat.geometry, 'click'); }

    selectedHighPointId = sendrId;
    EnableTools('highPoint');
    $.observable(highPointsJson).setProperty("selectedID", GetSelectedHighPointId());
  } catch (e) { HiUser(e, "Process Select High Point"); }
}
function SelectHighPoint(sendr, ev) {
  try {
    if (actionType) return;
    ClearTableSelections(featureTypes.HIGHPOINT);
    highPointMapOrTable = "table"; //selected from table, run map selection
    infowindow.close();
    infowindow = new GGLMAPS.InfoWindow();
    //validate new selection
    var isChecked = sendr.checked;
    if (isChecked && sendr.id === selectedHighPointId) return; //no change
    
    ProcessSelectHighPoint(sendr);
    //stopPropagation or else radio button becomes unselected
    ev.stopPropagation();
  } catch (e) { HiUser(e, "Select High Point"); }
}
function GetSelectedHighPointId() {
  var retVal = "";
  try {
    if (-1 == selectedHighPointId) return "";
    var idCtlName = selectedHighPointId.replace("Select", "Guid");
    var idCtl = $("#" + idCtlName + "");
    if (1 > idCtl.length) return ""; //not found
    retVal = idCtl.val();
  } catch (e) { HiUser(e, "Get Selected High Point Id"); }
  return retVal;
}
function AddHighPointPoint(latlng) {
  if (inDrawMode) {
//    if (actionType === actionTypes.ADD) {
      polyPoints = CreateMvcPointArray2(latlng.lng() + "," + latlng.lat());
      PrepareHighPoint(highPointStyles, 0);
      SubmitFeature();
//    } else if (actionType === actionTypes.EDIT) {
//      
//    }
  }
}

function StartDrawingHighPoint(sendr) {
  var okToCont = false;
  try {
    polyPoints = new GGLMAPS.MVCArray();
    var viewBnds = gglmap.getBounds();
    var featBnds = null;
    if (actionType === actionTypes.EDIT) {
      okToCont = true;
      preEditCoords = GetHighPointCoords(editFeat.highPointRecord);
      polyShape = editFeat.geometry;
      polyPoints.push(polyShape.getPosition());
      polyShape.setDraggable(true);
      if (preEditCoords.length > 0) featBnds = CreatePoint(preEditCoords);
      if (featBnds && !viewBnds.contains(featBnds) && preEditCoords.length > 0) { gglmap.setCenter(featBnds); }
    } else { //not editing 
      okToCont = true;
      // will PrepareHighPoint in click event
      AddClass(document.getElementById("uxCreateHighPointDrawSubmit"), "display-none");
    } // END: editing
    ShowToolsMainDiv(true);
  } catch (e) { HiUser(e, "Start Drawing"); }
  return okToCont;
}

function PreSelectHighPoint() { $("#uxHighPointSelect0").click(); }
//function EditHighPoint(sendr) {
//  try {
//    if (!editFeat || featureTypes.HIGHPOINT !== editingFeatType) { alert("Please select a high point first."); return false; }
//    EditFeature(sendr, featureTypes.HIGHPOINT, editingFeatIndx, editingOid);
//  } catch (err) { HiUser(err, "Edit High Point"); }
//}
function EditHighPoint(sendr) {
  try {
    if (myHighPoints.count > 0) {
      PreSelectHighPoint();
      window.setTimeout(EditHighPoint_Part2, 250, sendr);
    } else {
      HiUser("No high point exists.\n\nPlease create a high point first.");
      return;
    }

  } catch (err) { HiUser(err, "Edit High Point"); }
}
function EditHighPoint_Part2(sendr) {
  try {
    if (!editFeat || featureTypes.HIGHPOINT !== editingFeatType) { alert("Please select a high point first."); return false; }
    infowindow.close();
    EditFeature(sendr, featureTypes.HIGHPOINT, editingFeatIndx, editingOid);
  } catch (err) { HiUser(err, "Edit High Point 2"); }
}
function BeginNewHighPoint(sendr) {
  try {
    if (myHighPoints.count > 0) {
      alert("Only one high point is allowed. Please edit or delete the current high point if you want to change it.");
      return;
    }
    BeginNewFeature(sendr);
    featureType = featureTypes.HIGHPOINT;
    ShowFeatureTools(featureType, sendr);
    GetControlByTypeAndId("input", "uxHighPointDrawStart").click();
  } catch (e) { HiUser(e, "Begin New High Point"); }
}
function FinishSubmitHighPoint(closeForm, action) {
  FinishSubmitFeature(closeForm, action);
  ClearHighPointSelection();
  if (closeForm) CloseForm("ux" + action + "HighPoint");
}
function DeleteHighPoint(oid) {
  try {
    if (myHighPoints.count > 0) {
      oid = myHighPoints.features[0].highPointRecord.ObjectID;
      PreSelectHighPoint();
      window.setTimeout(DeleteHighPoint_Part2, 250, oid);
    } else {
      HiUser("No high point exists to delete.");
      return;
    }
  } catch (e) { HiUser(e, "Delete High Point"); }
}
function DeleteHighPoint_Part2(oid) {
  try {
    infowindow.close();
    if ("undefined" === typeof editingFeatType || featureTypes.HIGHPOINT !== editingFeatType
      || "undefined" === typeof editingOid || 0 > editingOid) { alert("Please select the high point first."); return false; }
    if ("undefined" === typeof oid || oid < 0) { alert("Please select the high point first."); return false; }
    var ovToDel = myHighPoints.GetFeatureByOid(oid);

    var confMsg = CR + CR;
    confMsg += "Are you sure you want to PERMANENTLY DELETE your high point: " + CR + CR + CR;
    var YorN = confirm(confMsg);
    if (YorN) {
      try {
        $("[id$=uxHighPointsInfo]").html("");
        var projId = GetProjId();
        // DeleteHighPoint(ByVal projectId As Integer, ByVal featureId As String) As NM.ReturnHighPointsStructure
        var svcData = "{featureId:'{1}',projectId:{2}}".replace("{1}", oid).replace("{2}", ParseInt10(projId));
        SetWebServiceIndicators(true, "Deleting High Point");
        $.ajax({
          url: "GISTools.asmx/DeleteHighPoint"
          , data: svcData
        })
        .done(function (data, textStatus, jqXHR) {
          ovToDel.Hide();
          highPointsJsonD = data.d;
          if (highPointsJsonD.info && highPointsJsonD.info.length > 0) HiUser(highPointsJsonD.info, "Delete High Point succeeded");
          LoadHighPointsDone();
          myHighPoints.SetFeatures();
          infowindow.close();
        })
        .fail(function (jqXHR, textStatus, errorThrown) {
          var errorResult = errorThrown;
          HiUser(errorResult, "Delete High Point failed.");
        })
        .always(function () {
          SetWebServiceIndicators(false);
          ClearHighPointSelection();
        });
        //    highPointsRetrievedIndx = editingFeatIndx; //store high point indx for reselection
      } catch (err) { HiUser(err, "Delete High Points"); SetWebServiceIndicators(false); }
    }
  } catch (e) { HiUser(e, "Delete High Point"); }
}
function CancelHighPointDraw(param) {
  try {
    if (actionType === actionTypes.EDIT && inDrawMode) {
      if (polyShape) polyShape.setMap(null);
      polyPoints = new GGLMAPS.MVCArray();
      if (editFeat) { SetHighPointGeometry(editFeat, preEditCoords, editingOid); myHighPoints.HighlightFeature(editingOid); }
      gglmap.setOptions({ disableDoubleClickZoom: false, draggableCursor: 'auto' });
      document.body.style.cursor = 'auto';
      cancelHighPointDrawHandled = true;
    } else if (actionType === actionTypes.EDIT) {
      if (cancelHighPointDrawHandled !== true) CancelDraw();
      cancelHighPointDrawHandled = true;
    } else {
      CancelDraw();
    }

    if (HasClass(GetControlByTypeAndId('div', 'uxCreateHighPointMain'), "display-none")) ShowToolsMainDiv(true);
    else HideHighPointTools();

    inDrawMode = false;
    SetDisplayStartDrawingButtons(true);
    if (param === "submitted") {
      SetVisibilityCss(GetControlByTypeAndId('input', 'uxHighPointAddNew'), true);
      SetStartDrawingButtonText("Edit Shape", "Edit the shape of the current feature");
      EditFeature(null, featureTypes.HIGHPOINT, editingFeatIndx, editingOid);
    }
    if (polyShape) polyShape.setMap(null);
    polyShape = null;
  } catch (e) { HiUser(e, "Cancel High Point Draw"); }
}
function HideHighPointTools() {
  SetDisplayCss(GetControlByTypeAndId("div", "uxCreateHighPointContainer"), false);
  CloseForm('uxEditHighPoint');
  HideFeatureTools();
}
function GetHighPointForWebService(action) {
  var features = {}; 
  return features; //nothing needed right now
}
function ShowHighPointTools(feattype, sendr) {
  try {
    var sendrId = sendr.id;
    if (sendrId.indexOf("Edit") > -1) {
      var feat = myHighPoints.GetFeatureByOid(editingOid);
      var add = document.getElementById("uxEditHighPointDrawNew");
      var mve = document.getElementById("uxEditHighPointDrawStart");
      if (feat.highPointRecord.Shape == "") { AddClass(mve, "display-none"); RemoveClass(add, "display-none"); }
      else { AddClass(add, "display-none"); RemoveClass(mve, "display-none"); }
    }

    inDrawMode = false;
    SetDisplayWithToolsOpen(false);
    var featdesc = "High Point";

    var toolsObj = GetControlByTypeAndId("div", "uxCreateHighPointContainer");
    if (actionType === actionTypes.EDIT) toolsObj = GetControlByTypeAndId("div", "uxEditHighPointContainer");
     
    SetDisplayCss(toolsObj, true); // show tools div
    ShowToolsMainDiv(true); // show options part of div
    if (actionType === actionTypes.ADD) ClearToolsFormsOptions(); // clear things if adding new
    featureGeometry = featureGeometrys[0];
    //SetFormBaseLocation(toolsObj,"uxHighPointsContainer"); 

    SetStartDrawingButtonText("Start Drawing", "Start drawing a new " + featdesc.toLowerCase());
    SetSubmitButtonText("Submit", "Submit the new " + featdesc.toLowerCase());

    if (actionType === actionTypes.EDIT) {
      SetStartDrawingButtonText("Start Drawing", "Draw a shape for the current feature");
      switch (feattype) {
        case featureTypes.HIGHPOINT:
          if (GetHighPointCoords(editFeat.highPointRecord).trim().length > 0) SetStartDrawingButtonText("Edit Shape", "Edit the shape of the current feature");
          OpenForm('uxEditHighPoint');
          break;
        default:
          HiUser("Feature type is not set.", "Show " + featdesc + " Tools");
          break;
      } // END: feattype check (switch)

      SetSubmitButtonVis(true);
      SetSubmitButtonText("Submit Edits", "Submit edits to database");
    }

  } catch (e) { HiUser(e, "Show High Point Tools"); }
}
function ReloadHighPoints() {
  try {
    $("[id$=uxHighPointsInfo]").html("");
    var infoMsg = "";
    SetWebServiceIndicators(true, "Getting High Points");
    var projid = GetProjId();
    var svcData = "{projectId:{0}}".replace("{0}", ParseInt10(projid, 10));
    $.ajax({
      url: "GISTools.asmx/GetHighPoints"
      , data: svcData
    })
    .done(function (msg) {
      highPointsJsonD = msg.d;
      if (highPointsJsonD.info && highPointsJsonD.info.length > 0) HiUser(highPointsJsonD.info, "Get High Points succeeded");
      LoadHighPointsDone();
      myHighPoints.SetFeatures();
    })
    .fail(function (jqXHR, textStatus, errorThrown) {
      var errorResult = errorThrown;
      HiUser(errorResult, "Get High Points failed.");
    })
    .always(function () {
      SetWebServiceIndicators(false);
      ClearHighPointSelection();
      $("#uxHighPointsInfo").html(infoMsg); //set after linking or it doesn't exist
    });
  } catch (e) { HiUser(e, "Load High Points"); SetWebServiceIndicators(false); }
}
function LoadHighPointsDone() {
  try {
    var info = "";
    if (!highPointsJsonD || !highPointsJsonD.highPoints || highPointsJsonD.highPoints.length === 0) info = "You do not have any high points created. Use the tools button to create a new high point.";

    ShowHighPoints();
    $("[id$=uxHighPointsInfo]").html(info); //set after linking or DNE
  } catch (e) { HiUser(e, "Load High Points Done"); }
}
function ShowHighPoints() {
  try {
    if (!highPointsJsonD || !highPointsJsonD.highPoints || highPointsJsonD.highPoints.length === 0) {
      highPointsJson = {};
      return;
    }
    var highPointsJsonHighPoints = highPointsJsonD.highPoints;

    highPointsJson = {
      highPoints: highPointsJsonHighPoints,
      selectedID: '0' /*highPointsJsonHighPoints.length > 0 ? highPointsJsonHighPoints[0].datumRecord.Guid : '0'*/,
      selected: function () {
        try {
          for (var i = 0; i < highPointsJsonHighPoints.length; i++) {
            if (highPointsJsonHighPoints[i].datumRecord.GUID === this.selectedID) {
              return highPointsJsonHighPoints[i];
            }
          }
        } catch (e) { HiUser(e, "Show High Points selected"); }
        return {};
      }
    };
    FleshOutHighPoints();

    highPointsJson.selected.depends = "selectedID";

    highPointsTmpl.link("#uxHighPointContainer", highPointsJson);
    editHighPointsTmpl.link("#uxEditHighPointContainer", highPointsJson);
    SetAccordions();
    $('input:radio[name*="HighPointSelect"]').off('click').on('click', function (e) { SelectHighPoint(this, e); });
  } catch (e) { HiUser(e, "Show High Points Link"); }
}
function GetHighPointCoords(featRec) {
  try {
    return featRec.Longitude + coordinateSplitter + featRec.Latitude;
  } catch (e) { HiUser(e, "Get High Point Coords"); }
}
function FleshOutHighPoints() {
  try {
    var feats = highPointsJson.highPoints;

    //add highPoint stuff
    var feat, featRec, featCoords;
    for (var f in feats) {
      feat = feats[f];
      featRec = feat.highPointRecord;
      if (!featRec) continue;
      feat.geometry = new GGLMAPS.Point();
      featCoords = GetHighPointCoords(featRec);
      SetHighPointGeometry(feat, featCoords, featRec.ObjectID);
      feat.Show = function () { this.geometry.setMap(gglmap); };
      feat.Hide = function () { this.geometry.setMap(null); };
    }
  } catch (e) { HiUser(e, "Flesh Out High Points"); }
}
function SetHighPointGeometry(feat, featCoords, featOid) {
  try {
    polyPoints = CreateMvcPointArray2(featCoords);
    PrepareHighPoint(highPointStyles, 0);
    feat.geometry = polyShape;
    feat.geometry.parent = featOid;
    feat.geometry.bounds = polyShape.getPosition();
    feat.geometry.center = GetCenterOfCoordsString(featCoords);
    AddFeatureClickEvents(feat.geometry, featureTypes.HIGHPOINT, featureGeometrys[0], featOid);
    ClearDrawingEntities();
  } catch (e) { HiUser(e, "Set High Point Geometry"); }
}
function ViewHighPoints(feat) {
  try {
    SetView("highPoint");
  } catch (e) { HiUser(e, "View High Points"); }
}

 