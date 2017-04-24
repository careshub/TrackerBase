/// <reference path="http://ajax.aspnetcdn.com/ajax/jQuery/jquery-1.5-vsdoc.js"/>
/// <reference path="Site.js" />

myContours = {
  count: 0
  , cls: 'Contour'
  , heading: 'Contour'
  , color: "#E69C24"
  , selColor: "#77F7E0" // "#1963DB"
  , geomType: "line"
  , features: {}
  , featureLabels: []
  , Init: function () {
    try { this.SetFeatures(); } catch (e) { HiUser(e, "Init Contours"); }
  }
  , Reset: function () {
    try {
      this.Hide();
      this.count = 0;
      this.features = {};
    } catch (e) { HiUser(e, "Reset Contours"); }
  }
  , SetFeatures: function () {
    try {
      this.Hide();
      if (!(contoursJson.contours)) { this.count = 0; return; }
      this.features = contoursJson.contours;
      this.count = this.features.length;
      this.Show();
    } catch (e) { HiUser(e, "Set Contours"); }
  }
  , GetFeatureName: function (oid) {
    try {
      var feat = this.GetFeatureByOid(oid);
      var featRec = feat.contourRecord;
      var name = featRec.ContourName;
      return name;
    } catch (e) { HiUser(e, "Get Contour Name"); }
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
    } catch (err) { HiUser(err, "Get Contour By Guid"); }
    return null;
  }
  , GetFeatureByOid: function (oid) {
    if (!oid || oid.length === 0) return null;
    oid = ParseInt10(oid);
    var feats = this.features;
    var featRec;
    try {
      for (var feat in feats) {
        featRec = feats[feat].contourRecord;
        if (featRec && oid == featRec.ObjectID) return feats[feat];
      }
    } catch (err) { HiUser(err, "Get Contour By Oid"); }
    return null;
  }
  , GetFeatureByName: function (contourName) {
    if (!contourName) return null;
    var feats = this.features;
    var featRec, featName;
    try {
      for (var feat in feats) {
        featRec = feats[feat].contourRecord;
        if (featRec) {
          featName = featRec.ContourName.toString().trim();
          if (featName == contourName) return feats[feat];
        }
      }
    } catch (err) { HiUser(err, "Get Contour By Name"); }
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
        featRec = feats[feat].contourRecord;
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
    } catch (err) { HiUser(err, "Get Contours Extent"); }
    if (haveAFeature) { retVal = newBounds; }
    return retVal;
  }
  , GetInfoWindow: function (oid) {
    var html;
    try {
      var feat = this.GetFeatureByOid(oid);
      var featRec = feat.contourRecord;
      html = "<div class='infoWin" + GetFeatureType(featureTypes.DIVIDE) + "' id='" + GetFeatureType(featureTypes.DIVIDE) + oid + "info'>";
      html += "<table class='" + this.cls.toLowerCase() + "Info' id='" + this.cls.toLowerCase() + oid + "'>";
      html += "<tr><th colspan='2' " +
        " style='background-color: " + this.selColor + ";' " +
        ">" + this.heading + "</th></tr>";

      var currData;
      currData = (featRec.Contour).toFixed(0);
      html += "<tr><td class='first'>" + "Elev (ft)" + ": </td>";
      html += "<td>" + unescape(currData);
      html += "</td></tr>";
      currData = (featRec.Length * ftPerMtr).toFixed(1);
      html += "<tr><td class='first'>" + "Length (ft)" + ": </td>";
      html += "<td>" + unescape(currData);
      html += "</td></tr>";

      html += "</table></div>";

    } catch (err) { HiUser(err, "Get Contour Info Window"); }
    return { content: html, position: feat.geometry.center };
  }
  , HighlightFeature: function (oid) {
    try {
      var feats = this.features;
      var featRec, featGeom;
      try {
        for (var featx in feats) {
          var feat = feats[featx];
          featRec = feat.contourRecord, featGeom = feat.geometry;
          if (!featRec) continue;
          var featOid = featRec.ObjectID;
          var featType = featRec.Type;
          var stroke = myContours.color;
          if (featOid.toString() != oid.toString() &&
                featGeom.strokeColor.toLowerCase() === contourStrokeHighlight.toLowerCase()) {
            featGeom.setOptions({ strokeColor: stroke });
          }
        }
      } catch (err) { HiUser(err, "Dehighlight Contour"); }
      var feat0 = this.GetFeatureByOid(oid);
      featGeom = feat0.geometry;
      var high = myContours.selColor;
      featGeom.setOptions({ strokeColor: high });
    } catch (err) { HiUser(err, "Highlight Contour"); }
  }
  , RemoveHighlights: function () {
    var feats = this.features;
    var featGeom, featRec;
    try {
      for (var feat in feats) {
        featGeom = feats[feat].geometry;
        featRec = feats[feat].contourRecord;
        var featType = featRec.Type;
        var stroke = myContours.color;
        if (featGeom) featGeom.setOptions({ strokeColor: stroke });
      }
    } catch (err) { HiUser(err, "Remove Contour Highlights"); }
  }
  , Hide: function (oid) {
    try {
      if (oid) { this.GetFeatureByOid(oid).Hide(); }
      else {
        var feats = this.features;
        for (var feat in feats) if (feats[feat] && typeof feats[feat].Hide === 'function') feats[feat].Hide();
      }
    } catch (err) { HiUser(err, "Hide Contours"); }
  }
  , Show: function (oid) {
    try {
      if (oid) { this.GetFeatureByOid(oid).Show(); }
      else {
        var feats = this.features;
        for (var feat in feats) if (feats[feat] && typeof feats[feat].Show === 'function') feats[feat].Show();
      }
    } catch (err) { HiUser(err, "Show Contours"); }
  }
  , GetLabel: function (oid) {
    var retVal = "not found";
    try {
      var feat = this.GetFeatureByOid(oid);
      var featRec = feat.contourRecord;
      var name;
      name = featRec.ContourName;
      retVal = name.toString();
    } catch (err) { HiUser(err, "Get Contour Label"); }
    return retVal;
  }
  , ToggleLabel: function (sendr) {
    try {
      if (sendr.checked) this.ShowLabel();
      else this.HideLabel();
    } catch (err) { HiUser(err, "Toggle Contour Label"); }
  }
  , HideLabel: function (sendr) {
    try {
      var lblsLen = this.featureLabels.length;
      for (var lblIdx = 0; lblIdx < lblsLen; lblIdx++) { this.featureLabels[lblIdx].hide(); }
    } catch (err) { HiUser(err, "Hide Contour Labels"); }
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
        featRec = feats[feat].contourRecord;
        if (featRec) {
          labelPos = feats[feat].geometry.center;
          if (!labelPos) continue;
          name = "ID: " + featRec.ContourName;
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
    } catch (e) { HiUser(e, "Show Contour Labels"); }
  }
}

var contoursJson, contoursJsonD;
var contourStrokeColor = myContours.color;
var contourStrokeWeight = 3;
var contourStrokeOpacity = 1.0;
var contourZIndex = 6;
var contourStrokeHighlight = myContours.selColor;

var cancelContourDrawHandled = false;

var contourStyles = [];

function ContourStyle() {
  this.name = "Contour";
  this.color = contourStrokeColor;
  this.width = contourStrokeWeight;
  this.lineopac = contourStrokeOpacity;
  this.zindex = contourZIndex;
}
function CreateContourStyleObject() {
  var linestyle = new ContourStyle(); contourStyles.push(linestyle);
  var tmpStrokeColor = contourStrokeColor;
  contourStrokeColor = contourStrokeHighlight; linestyle = new ContourStyle(); contourStyles.push(linestyle);
  contourStrokeColor = tmpStrokeColor;
}
function PrepareContour(styleArray, styleIndx) {
  try {
    if (!styleArray) styleArray = contourStyles;
    if (!styleIndx) styleIndx = 0;
    //console.log("PrepareContour polyPoints", polyPoints);
    var polyOptions = {
      path: polyPoints
    , strokeColor: styleArray[styleIndx].color
    , strokeOpacity: styleArray[styleIndx].lineopac
    , strokeWeight: styleArray[styleIndx].width
    , zIndex: styleArray[styleIndx].zindex
    };
    polyShape = new GGLMAPS.Polyline(polyOptions);
    polyShape.setMap(gglmap);
  } catch (e) { HiUser(e, "Prepare Contour"); }
}

function InitializeContours() {
  CreateContourStyleObject();
  if (contoursJson) {
    contoursJsonD = contoursJson.d; //set as if web service call
    LoadContoursDone();
    myContours.Init();
  }
}
function ToggleContours(sendr) {
  var show = sendr.checked;
  if (show) myContours.Show();
  else myContours.Hide();
}

function ClearContourSelection(params) {
  try {
    myContours.RemoveHighlights();
    ClearEditFeat();
  } catch (e) { HiUser(e, "Clear Contour Selection"); }
}
var contourMapOrTable; //track where selection was made
var selectedContourId;
function SelectContourInMap(oid) { try { FeatureClickFunction(featureTypes.CONTOUR, oid); } catch (e) { HiUser(e, "Select Contour In Map"); } }
function SelectContourInTable(oid) {
  try {
    var sels = $("[id*='uxContourOid']");
    var ids = "", $this, thisid, sendrId = "";
    sels.each(function () { // Iterate over items
      $this = $(this);
      thisid = $this.attr("id");
      if ($this.val() == oid) sendrId = thisid.replace("Oid", "Select");
    });
    if (sendrId !== "") {
      var sendr = GetControlByTypeAndId("input", sendrId);
      ProcessSelectContour(sendr);
    }
  } catch (e) { HiUser(e, "Select Contour In Table"); }
}
function ProcessSelectContour(sendr) {
  try {
    var sendrId = sendr.id;
    //turn off other selections, reset css
    $('#' + sendrId).prop("checked", true);
    $('#uxContourContainer input[type="radio"]:not(#' + sendrId + ')').prop("checked", false);
    $('#uxContourContainer input[type="radio"]:not(#' + sendrId + ')').parent().removeClass("accord-header-highlight");
    //highlight selection
    $(sendr).parent().addClass("accord-header-highlight");

    var oid = $("#" + sendr.id.replace("Select", "Oid")).val();
    var feat = myContours.GetFeatureByOid(oid);

    if ("table" === contourMapOrTable) { GGLMAPS.event.trigger(feat.geometry, 'click', {}); }
    //    if ("table" === contourMapOrTable) FeatureClickListener(feat, featureTypes[0], featureGeometrys[2], oid, google.maps.event.trigger(feat, 'click'));

    selectedContourId = sendrId;
    EnableTools('contour');
    $.observable(contoursJson).setProperty("selectedID", GetSelectedContourId());
  } catch (e) { HiUser(e, "Process Select Contour"); }
}

function SelectContour(sendr, ev) {
  try {
    if (actionType) return;
    ClearTableSelections(featureTypes.CONTOUR);
    contourMapOrTable = "table"; //selected from table, run map selection
    infowindow.close();
    infowindow = new GGLMAPS.InfoWindow();
    //validate new selection
    var isChecked = sendr.checked;
    if (isChecked && sendr.id === selectedContourId) return; //no change

    ProcessSelectContour(sendr);
    //stopPropagation or else radio button becomes unselected
    ev.stopPropagation();
  } catch (e) { HiUser(e, "Select Contour"); }
}
function GetSelectedContourId() {
  var retVal = "";
  try {
    if (-1 == selectedContourId) return "";
    var idCtlName = selectedContourId.replace("Select", "Guid");
    var idCtl = $("#" + idCtlName + "");
    if (1 > idCtl.length) return ""; //not found
    retVal = idCtl.val();
  } catch (e) { HiUser(e, "Get Selected Contour Id"); }
  return retVal;
}
function GetContourNameFromUserInput(action) {
  var contourId = GetControlByTypeAndId("input", "ux" + action + "ContourContourName").value.trim();
  return contourId;
}
function PreSelectContour() {
  $("#uxContourSelect0").click();
}
function EditContour(sendr) {
  try {
    if (myContours.count > 0) {
      if (!editFeat || featureTypes.CONTOUR !== editingFeatType) { alert("Please select a contour first."); return false; }
      else EditContour_Part2(sendr);
    } else {
      HiUser("No contour exists.\n\nPlease create a contour first.");
      return;
    }
  } catch (err) { HiUser(err, "Edit Contour"); }
}
function EditContour_Part2(sendr) {
  try {
    if (!editFeat || featureTypes.CONTOUR !== editingFeatType) { alert("Please select a contour first."); return false; }
    infowindow.close();
    LoadContourOptions("Edit");
    EditFeature(sendr, featureTypes.CONTOUR, editingFeatIndx, editingOid);
  } catch (err) { HiUser(err, "Edit Contour 2"); }
}
function BeginNewContour(sendr) {
  try {
    BeginNewFeature(sendr);
    featureType = featureTypes.CONTOUR;
    ClearContourToolsForm();
    ShowFeatureTools(featureType, sendr);
  } catch (e) { HiUser(e, "Begin New Contour"); }
}
function LoadContourOptions(action) { }
function FinishSubmitContour(closeForm, action) {
  FinishSubmitFeature(closeForm, action);
  ClearContourSelection();
  if (closeForm) CloseForm("ux" + action + "Contour");
  inDrawMode = false;
}

function DeleteContours(sendr) {
  try {
    if (myContours.count == 0 && myContourRaws.count == 0) {
      alert("There are no contours to delete."); return false;
    } else DeleteContours_Part2(sendr);
  } catch (e) { HiUser(e, "Delete Contours"); }
}
function DeleteContours_Part2(sendr) {
  try {
    infowindow.close(); 

    var confMsg = CR + CR;
    confMsg += "Are you sure you want to delete all raw and smoothed contours?" + CR;
    confMsg += CR + CR;
    var YorN = confirm(confMsg);
    if (YorN) {
      try {
        $("[id$=uxContoursInfo]").html("");
        $("[id$=uxContourRawsInfo]").html("");
        var projId = GetProjId();
        var svcData = {};
        svcData["projectId"] = projId;
        SetWebServiceIndicators(true, "Deleting Contours");
        $.ajax({
          url: "GISTools.asmx/DeleteAllContours"
          , data: JSON.stringify(svcData)
        })
        .done(function (data, textStatus, jqXHR) {
          myContourRaws.Reset();
          myContours.Reset();
          contoursJsonD = contourRawsJsonD = data.d;
          if (contoursJsonD.info && contoursJsonD.info.length > 0) HiUser(contoursJsonD.info, "Delete Contours succeeded");
          //LoadContoursDone();
          //myContours.SetFeatures();
          //LoadContourRawsDone();
          //myContourRaws.SetFeatures();
          infowindow.close();
        })
        .fail(function (jqXHR, textStatus, errorThrown) {
          var errorResult = errorThrown;
          HiUser(errorResult, "Delete Contours failed.");
        })
        .always(function () {
          SetWebServiceIndicators(false);
          ClearContourSelection();
          ClearContourRawSelection();
        });
        //    contoursRetrievedIndx = editingFeatIndx; //store contour indx for reselection
      } catch (err) { HiUser(err, "Delete Contours 2"); SetWebServiceIndicators(false); }
    }
  } catch (e) { HiUser(e, "Delete Contours 2"); }
}
function DeleteContour(sendr) {
  try {
    if (myContours.count > 0) {
      if (!editFeat || featureTypes.CONTOUR !== editingFeatType) { alert("Please select a contour first."); return false; }
      else DeleteContour_Part2(sendr);
    } else {
      HiUser("No contour exists to delete.");
      return;
    }
  } catch (e) { HiUser(e, "Delete Contour"); }
}
function DeleteContour_Part2(sendr) {
  try {
    infowindow.close();
    if ("undefined" === typeof editingFeatType || featureTypes.CONTOUR !== editingFeatType
      /*|| "undefined" === typeof editingFeatIndx || 0 > editingFeatIndx*/ || "undefined" === typeof editingOid || 0 > editingOid) { alert("Please select a contour first"); return false; }
    if ("undefined" === typeof editingOid || editingOid < 0) { alert("Please select a contour first."); return false; }
    var ovToDel = myContours.GetFeatureByOid(editingOid);

    var confMsg = CR + CR;
    confMsg += "Are you sure you want to delete this contour?" + CR;
    confMsg += CR + CR;
    var YorN = confirm(confMsg);
    if (!YorN) { myContours.RemoveHighlights(); }
    else if (YorN) {
      try {
        $("[id$=uxContoursInfo]").html("");
        var projId = GetProjId();
        var svcData = "{projectId:{1},id:'{2}'}".replace("{1}", ParseInt10(projId)).replace("{2}", editingOid);
        SetWebServiceIndicators(true, "Deleting Contour");
        $.ajax({
          url: "GISTools.asmx/DeleteContour"
          , data: svcData
        })
        .done(function (data, textStatus, jqXHR) {
          ovToDel.geometry.setMap(null);
          contoursJsonD = data.d;
          if (contoursJsonD.info && contoursJsonD.info.length > 0) HiUser(contoursJsonD.info, "Delete Contour succeeded");
          LoadContoursDone();
          myContours.SetFeatures();
          infowindow.close();
        })
        .fail(function (jqXHR, textStatus, errorThrown) {
          var errorResult = errorThrown;
          HiUser(errorResult, "Delete Contour failed.");
        })
        .always(function () {
          SetWebServiceIndicators(false);
          ClearContourSelection();
        });
        //    contoursRetrievedIndx = editingFeatIndx; //store contour indx for reselection
      } catch (err) { HiUser(err, "Delete Contours 2"); SetWebServiceIndicators(false); }
    }
  } catch (e) { HiUser(e, "Delete Contour 2"); }
}
function ShowContourTools(feattype, sendr) {
  try {
    var sendrId = sendr.id;
    if (sendrId.indexOf("Edit") > -1) {
      var feat = myContours.GetFeatureByOid(editingOid);
      var add = document.getElementById("uxEditContourDrawNew");
      var mve = document.getElementById("uxEditContourDrawStart");
      if (feat.contourRecord.Shape == "") { AddClass(mve, "display-none"); RemoveClass(add, "display-none"); }
      else { AddClass(add, "display-none"); RemoveClass(mve, "display-none"); }
    }

    inDrawMode = false;
    SetDisplayWithToolsOpen(false);
    var featdesc = "Contour";

    var toolsObj = GetControlByTypeAndId("div", "uxCreateContourContainer");
    if (actionType === actionTypes.EDIT) toolsObj = GetControlByTypeAndId("div", "uxEditContourContainer");
     
    SetDisplayCss(toolsObj, true); // show tools div
    ShowToolsMainDiv(true); // show options part of div
    if (actionType === actionTypes.ADD) ClearToolsFormsOptions(); // clear things if adding new
    featureGeometry = featureGeometrys[1];

    //SetFormBaseLocation(toolsObj,"uxContoursContainer");

    SetStartDrawingButtonText("Start Drawing", "Start drawing a new " + featdesc.toLowerCase());
    SetSubmitButtonText("Submit", "Submit the new " + featdesc.toLowerCase());

    if (actionType === actionTypes.EDIT) {
      SetStartDrawingButtonText("Start Drawing", "Draw a shape for the current feature");
      switch (feattype) {
        case featureTypes.CONTOUR:
          if (editFeat.contourRecord.Coords.trim().length > 0) SetStartDrawingButtonText("Edit Shape", "Edit the shape of the current feature");
          OpenForm('uxEditContour');
          break;
        default:
          HiUser("Feature type is not set.", "Show " + featdesc + " Tools");
          break;
      } // END: feattype check (switch)

      SetSubmitButtonVis(true);
      SetSubmitButtonText("Submit Edits", "Submit edits to database");
    }

  } catch (e) { HiUser(e, "Show Contour Tools"); }
}
function CancelContourDraw(param) {
  try {
    if (actionType === actionTypes.EDIT && inDrawMode) {
      if (polyShape) polyShape.setMap(null);
      polyPoints = new GGLMAPS.MVCArray();
      if (editFeat) { SetContourGeometry(editFeat, preEditCoords, editingOid); myContours.HighlightFeature(editingOid); }
      gglmap.setOptions({ disableDoubleClickZoom: false, draggableCursor: 'auto' });
      document.body.style.cursor = 'auto';
      cancelContourDrawHandled = true;
    } else if (actionType === actionTypes.EDIT) {
      if (cancelContourDrawHandled !== true) CancelDraw();
      cancelContourDrawHandled = true;
    } else {
      CancelDraw();
    }

    if (HasClass(GetControlByTypeAndId('div', 'uxCreateContourMain'), "display-none")) ShowToolsMainDiv(true);
    else HideContourTools();

    inDrawMode = false;
    SetDisplayStartDrawingButtons(true);
    if (param === "submitted") {
      SetVisibilityCss(GetControlByTypeAndId('input', 'uxContourAddNew'), true);
      SetStartDrawingButtonText("Edit Shape", "Edit the shape of the current feature");
      EditFeature(null, featureTypes.HIGHPOINT, editingFeatIndx, editingOid);
    }
    if (polyShape) polyShape.setMap(null);
    polyShape = null;
  } catch (e) { HiUser(e, "Cancel Contour Draw"); }
}
function HideContourTools() {
  SetDisplayCss(GetControlByTypeAndId("div", "uxCreateContourContainer"), false);
  CloseForm('uxEditContour');
  HideFeatureTools();
}
function ClearContourToolsForm() {
  try {
  } catch (e) { HiUser(e, "Clear Contour Tools Form"); }
}
function GetContourForWebService(action) {
  var features = {};
  return features; //nothing needed right now

  var json, strCount = 0, datatypes = "";
  try {
    var features = {};    // Create empty javascript object
    var $this, thisid, attr, dataType, dte, replcVal = "ux" + action + "Contour";
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
    if (!features["ContourName"]) features["ContourName"] = GetProjectName();
    json = JSON.stringify(features); // Stringify to create json object
  } catch (e) { HiUser(e, "Get Contour For Web Service"); return null; }
  return features;
}
function ReloadContours() {
  try {
    $("[id$=uxContoursInfo]").html("");
    SetWebServiceIndicators(true, "Getting contours");
    var projId = GetProjId();
    var svcData = "{projectId:{0}}".replace("{0}", ParseInt10(projId));
    $.ajax({
      url: "GISTools.asmx/GetContours"
      , data: svcData
    })
    .done(function (data, textStatus, jqXHR) {
      contoursJsonD = data.d;
      if (contoursJsonD.info && contoursJsonD.info.length > 0) HiUser(contoursJsonD.info, "Get Contours succeeded");
      LoadContoursDone();
      myContours.SetFeatures();
    })
    .fail(function (jqXHR, textStatus, errorThrown) {
      var errorResult = errorThrown;
      HiUser(errorResult, "Get Contours failed.");
    })
    .always(function () {
      SetWebServiceIndicators(false);
      ClearContourSelection();
    });
    //    contoursRetrievedIndx = editingFeatIndx; //store contour indx for reselection
  } catch (err) { HiUser(err, "Load Contours"); SetWebServiceIndicators(false); }
}
function LoadContoursDone() {
  try {
    var info = "";
    if (!contoursJsonD || !contoursJsonD.contours || contoursJsonD.contours.length === 0) info = "You do not have any contours created. Use the tools button to create a new contour.";

    RenderContours();
    $("[id$=uxContoursInfo]").html(info); //set after linking or DNE
  } catch (e) { HiUser(e, "Load Contours Done"); }
}
function RenderContours() {
  try {
    if (!contoursJsonD || !contoursJsonD.contours || contoursJsonD.contours.length === 0) return;
    var contoursJsonContours = contoursJsonD.contours;

    contoursJson = {
      contours: contoursJsonContours
      , selectedID: (contoursJsonContours && contoursJsonContours.length > 0) ? contoursJsonContours[0].datumRecord.GUID : '0'
      , selected: function () {
        try {
          for (var i = 0; i < contoursJsonContours.length; i++) {
            if (contoursJsonContours[i].datumRecord.GUID === this.selectedID) {
              return contoursJsonContours[i];
            }
          }
        } catch (e) { HiUser(e, "Show Contours selected"); }
        return {};
      }
    };
    FleshOutContours();

    contoursJson.selected.depends = "selectedID";

    //contoursTmpl.link("#uxContourContainer", contoursJson);
    //editContoursTmpl.link("#uxEditContourContainer", contoursJson);
    SetAccordions();
    $('input:radio[name*="ContourSelect"]').off('click').on('click', function (e) { SelectContour(this, e); });
  } catch (e) { HiUser(e, "Render Contours"); }
}
function FleshOutContours() {
  try {
    var feats = contoursJson.contours;
    var feat, featRec;
    for (var f in feats) {
      feat = feats[f];
      featRec = feat.contourRecord;
      if (!featRec) continue;
      feat.geometry = new GGLMAPS.Polyline();
      SetContourGeometry(feat, featRec.Coords, featRec.ObjectID);
      feat.Show = function () { this.geometry.setMap(gglmap); };
      feat.Hide = function () { this.geometry.setMap(null); };
    }
  } catch (e) { HiUser(e, "Flesh Out Contours"); }
}
function SetContourGeometry(feat, featCoords, featOid) {
  try {
    polyPoints = CreateMvcPointArray(featCoords);
    var styleIx = 0;
    PrepareContour(contourStyles, styleIx);
    feat.geometry = polyShape;
    feat.geometry.parent = featOid;
    feat.geometry.bounds = GetBoundsForPoly(polyShape);
    feat.geometry.center = GetCenterOfCoordsString(featCoords);
    AddFeatureClickEvents(feat.geometry, featureTypes.CONTOUR, myContours.geomType, featOid);
    ClearDrawingEntities();
  } catch (e) { HiUser(e, "Set Contour Geometry"); }
}

function CalculateContours(sendr) {
  try {
    console.log(sendr);
    var isContours = (ParseInt10(myContours.count) > 0) ? true : false;
    if (isContours) {
      var overwrite = confirm("Do you wish to delete your current contours and calculate new ones?");
      if (!overwrite) return;
    }
    myContours.Reset();

    var projId = GetProjId();

    var svcData = {};
    svcData["projectId"] = ParseInt10(projId, 10);

    //SetWebServiceIndicators(true, "Calculating Contours");
    var origVal = sendr.value;
    sendr.value = "Processing...";
    sendr.setAttribute("disabled", true);
    myContours.Reset();
    //  Public Function CalculateContours(ByVal projectId As Long) As String
    $.ajax({
      url: "GISTools.asmx/CalculateContours"
      , data: JSON.stringify(svcData)
    })
    .done(function (data, textStatus, jqXHR) {
      var msg = "";
      if (data && data.d) msg = data.d;
      if (msg.trim() !== "") HiUser(msg);
    })
    .fail(function (jqXHR, textStatus, errorThrown) {
      var msg = "";
      if (textStatus) msg += "Status: " + textStatus;
      if (errorThrown) msg += "\nError: " + errorThrown;
      HiUser(msg, "Calculate Contours failed or timed out.");
    })
    .always(function () {
      SetWebServiceIndicators(false);
      sendr.value = origVal;
      sendr.removeAttribute("disabled");
    });
  } catch (e) { HiUser(e, "Submit Calculate Contours"); SetWebServiceIndicators(false); }
}
function LoadSmoothContours() {
  try { 
    var projId = GetProjId();

    var svcData = {};
    svcData["projectId"] = ParseInt10(projId, 10);

    SetWebServiceIndicators(true, "Loading Smoothed Contours");
    myContours.Reset();
    //  Public Function LoadSmoothContours(ByVal projectId As Long) As ContourPackageList
    $.ajax({
      url: "GISTools.asmx/LoadSmoothContours"
      , data: JSON.stringify(svcData)
    })
    .done(function (data, textStatus, jqXHR) {
      contoursJsonD = data.d;
      if (contoursJsonD.info && contoursJsonD.info.trim().length > 0) HiUser(contoursJsonD.info, "Load Smooth Contours succeeded");
      if (!contoursJsonD.contours) { myContours.Reset(); HiUser("No smooth contours are available", "Load Smooth succeeded"); }
      LoadContoursDone();
      myContours.SetFeatures();
    })
    .fail(function (jqXHR, textStatus, errorThrown) {
      var msg = "";
      if (textStatus) msg += "Status: " + textStatus;
      if (errorThrown) msg += "\nError: " + errorThrown;
      HiUser(msg, "Load Smooth Contours failed.");
    })
    .always(function () {
      SetWebServiceIndicators(false);
    });
  } catch (e) { HiUser(e, "Submit Load Smooth Contours"); SetWebServiceIndicators(false); }
}
