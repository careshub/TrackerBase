/// <reference path="http://ajax.aspnetcdn.com/ajax/jQuery/jquery-1.5-vsdoc.js"/>
/// <reference path="Site.js" />

myWaterways = {
  count: 0
  , cls: 'Waterway'
  , heading: 'Waterway'
  , color: "#0000FF"
  , selColor: "#77F7E0" //  "#FFFF00"
  , features: {}
  , featureLabels: []
  , Init: function () {
    try { this.SetFeatures(); } catch (e) { HiUser(e, "Init Waterways"); }
  }
  , SetFeatures: function () {
    try {
      this.Hide();
      if (!waterwaysJson || !(waterwaysJson.waterways)) { this.features = {}; this.count = 0; return; }
      this.features = waterwaysJson.waterways;
      this.count = this.features.length;
      this.Show();
    } catch (e) { HiUser(e, "Set Waterways"); }
  }
  , GetFeatureName: function (oid) {
    try {
      var feat = this.GetFeatureByOid(oid);
      var featRec = feat.waterwayRecord;
      var name = featRec.WaterwayName;
      return name;
    } catch (e) { HiUser(e, "Get Waterway Name"); }
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
    } catch (err) { HiUser(err, "Get Waterway By Guid"); }
    return null;
  }
  , GetFeatureByOid: function (oid) {
    if (!oid || oid.length === 0) return null;
    oid = ParseInt10(oid);
    var feats = this.features;
    var featRec;
    try {
      for (var feat in feats) {
        featRec = feats[feat].waterwayRecord;
        if (featRec && oid == featRec.ObjectID) return feats[feat];
      }
    } catch (err) { HiUser(err, "Get Waterway By Oid"); }
    return null;
  }
  , GetFeatureByName: function (waterwayName) {
    if (!waterwayName) return null;
    var feats = this.features;
    var featRec, featName;
    try {
      for (var feat in feats) {
        featRec = feats[feat].waterwayRecord;
        if (featRec) {
          featName = featRec.WaterwayName.toString().trim();
          if (featName == waterwayName) return feats[feat];
        }
      }
    } catch (err) { HiUser(err, "Get Waterway By Name"); }
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
        featRec = feats[feat].waterwayRecord;
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
    } catch (err) { HiUser(err, "Get Waterways Extent"); }
    if (haveAFeature) { retVal = newBounds; }
    return retVal;
  }
  , GetInfoWindow: function (oid) {
    var html;
    try {
      var feat = this.GetFeatureByOid(oid);
      var featRec = feat.waterwayRecord;
      html = "<div class='infoWin" + GetFeatureType(featureTypes.WATERWAY) + "' id='" + GetFeatureType(featureTypes.WATERWAY) + oid + "info'>";
      html += "<table class='" + this.cls.toLowerCase() + "Info' id='" + this.cls.toLowerCase() + oid + "'>";
      html += "<tr><th colspan='2' " +
        " style='background-color: " + this.selColor + ";' " +
        ">" + this.heading + "</th></tr>";

      var currData;
      currData = (featRec.Length * ftPerMtr).toFixed(1);
      html += "<tr><td class='first'>" + "Length (ft)" + ": </td>";
      html += "<td>" + unescape(currData);
      html += "</td></tr>";
      currData = (featRec.Ordinal);
      html += "<tr><td>" + "Index" + ": </td>";
      html += "<td>" + unescape(currData);
      html += "</td></tr>";

      html += "</table></div>";

    } catch (err) { HiUser(err, "Get Waterway Info Window"); }
    return { content: html, position: feat.geometry.center };
  }
  , HighlightFeature: function (oid) {
    try {
      var feats = this.features;
      var featRec, featGeom;
      try {
        for (var featx in feats) {
          var feat = feats[featx];
          featRec = feat.waterwayRecord, featGeom = feat.geometry;
          if (!featRec) continue;
          var featOid = featRec.ObjectID;
          if (featOid.toString() != oid.toString() &&
                  featGeom.strokeColor.toLowerCase() === waterwayStrokeHighlight.toLowerCase()) {
            featGeom.setOptions({ strokeColor: waterwayStrokeColor, zIndex: waterwayZIndex });
          }
        }
      } catch (err) { HiUser(err, "Dehighlight Waterway"); }
      var feat0 = this.GetFeatureByOid(oid);
      featGeom = feat0.geometry;
      featGeom.setOptions({ strokeColor: waterwayStrokeHighlight, zIndex: waterwayZIndex + 1 });
    } catch (err) { HiUser(err, "Highlight Waterway"); }
  }
  , RemoveHighlights: function () {
    var feats = this.features;
    var featGeom;
    try {
      for (var feat in feats) {
        featGeom = feats[feat].geometry;
        if (featGeom) featGeom.setOptions({ strokeColor: waterwayStrokeColor, zIndex: waterwayZIndex });
      }
    } catch (err) { HiUser(err, "Remove Waterway Highlights"); }
  }
  , Hide: function (oid) {
    try {
      if (oid) { this.GetFeatureByOid(oid).Hide(); }
      else {
        var feats = this.features;
        for (var feat in feats) if (feats[feat] && typeof feats[feat].Hide === 'function') feats[feat].Hide();
      }
    } catch (err) { HiUser(err, "Hide Waterways"); }
  }
  , Show: function (oid) {
    try {
      if (oid) { this.GetFeatureByOid(oid).Show(); }
      else {
        var feats = this.features;
        for (var feat in feats) if (feats[feat] && typeof feats[feat].Show === 'function') feats[feat].Show();
      }
    } catch (err) { HiUser(err, "Show Waterways"); }
  }
  , GetLabel: function (oid) {
    var retVal = "not found";
    try {
      var feat = this.GetFeatureByOid(oid);
      var featRec = feat.waterwayRecord;
      var name;
      name = featRec.WaterwayName;
      retVal = name.toString();
    } catch (err) { HiUser(err, "Get Waterway Label"); }
    return retVal;
  }
  , ToggleLabel: function (sendr) {
    try {
      if (sendr.checked) this.ShowLabel();
      else this.HideLabel();
    } catch (err) { HiUser(err, "Toggle Waterway Label"); }
  }
  , HideLabel: function (sendr) {
    try {
      var lblsLen = this.featureLabels.length;
      for (var lblIdx = 0; lblIdx < lblsLen; lblIdx++) { this.featureLabels[lblIdx].hide(); }
    } catch (err) { HiUser(err, "Hide Waterway Labels"); }
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
        featRec = feats[feat].waterwayRecord;
        if (featRec) {
          labelPos = feats[feat].geometry.center;
          if (!labelPos) continue;
          name = "ID: " + featRec.WaterwayName;
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
    } catch (e) { HiUser(e, "Show Waterway Labels"); }
  }
  , GetMaxOrdinal: function () {
    var retVal = 0;
    var feats = this.features;
    var featRec, featIndex;
    try {
      for (var feat in feats) {
        featRec = feats[feat].waterwayRecord;
        if (featRec) {
          featIndex = ParseInt10(featRec.Ordinal);
          if (featIndex > retVal) retVal = featIndex;
        }
      }
    } catch (e) { HiUser(e, "Get Max Ordinal Waterway"); }
    return retVal;
  }
  , Sort: function (key, descTorF) {
    var retVal = [];
    try {
      var cnt = 0;
      var feats = this.features;
      for (var prop in feats) {
        if (feats.hasOwnProperty(prop) && "waterwayRecord" in feats[prop]) {
          cnt++; retVal.push(feats[prop]);
        }
      }
      retVal.sort(function (a, b) { //sort
        var retVal = a["waterwayRecord"][key] > b["waterwayRecord"][key];
        if (descTorF) retVal = retVal * -1;
        return retVal;
      });
    } catch (err) { HiUser(err, "Sort Waterways"); }
    return retVal;
  }
}

var waterwaysJson, waterwaysJsonD;
var waterwayStrokeColor = myWaterways.color;
var waterwayStrokeWeight = 4;
var waterwayStrokeOpacity = 1.0;
var waterwayZIndex = 9;
var waterwayStrokeHighlight = myWaterways.selColor;

var cancelWaterwayDrawHandled = false;

var waterwayStyles = [];

function WaterwayStyle() {
  this.name = "Waterway";
  this.color = waterwayStrokeColor;
  this.width = waterwayStrokeWeight;
  this.lineopac = waterwayStrokeOpacity;
  this.zindex = waterwayZIndex;
}
function CreateWaterwayStyleObject() {
  var linestyle = new WaterwayStyle(); waterwayStyles.push(linestyle);
  var tmpStrokeColor = waterwayStrokeColor;
  waterwayStrokeColor = waterwayStrokeHighlight;
  linestyle = new WaterwayStyle(); waterwayStyles.push(linestyle);
  waterwayStrokeColor = tmpStrokeColor;
}
function PrepareWaterway(styleArray, styleIndx) {
  try {
    if (!styleArray) styleArray = waterwayStyles;
    if (!styleIndx) styleIndx = 0;
    //console.log("PrepareWaterway polyPoints", polyPoints);
    var polyOptions = {
      path: polyPoints
    , strokeColor: styleArray[styleIndx].color
    , strokeOpacity: styleArray[styleIndx].lineopac
    , strokeWeight: styleArray[styleIndx].width
    , zIndex: styleArray[styleIndx].zindex
    };
    polyShape = new GGLMAPS.Polyline(polyOptions);
    polyShape.setMap(gglmap);
  } catch (e) { HiUser(e, "Prepare Waterway"); }
  try {
    //if (!styleArray) styleArray = waterwayStyles;
    //if (!styleIndx) styleIndx = 0;
    //console.log("PrepareWaterway polyPoints", polyPoints);
    //var polyOptions = {
    //  path: polyPoints
    //, strokeColor: styleArray[styleIndx].color
    //, strokeOpacity: 0.01
    //, strokeWeight: 25
    //, zIndex: styleArray[styleIndx].zindex - 1
    //};
    //polyShape = new GGLMAPS.Polyline(polyOptions);
    //polyShape.setMap(gglmap);
  } catch (e) { HiUser(e, "Prepare Waterway Selector"); }
}

function InitializeWaterways() {
  CreateWaterwayStyleObject();
  if (waterwaysJson) {
    waterwaysJsonD = waterwaysJson.d; //set as if web service call
    LoadWaterwaysDone();
    myWaterways.Init();
  }
}

function ClearWaterwaySelection(params) {
  try {
    myWaterways.RemoveHighlights();
    ClearEditFeat();
  } catch (e) { HiUser(e, "Clear Waterway Selection"); }
}
var waterwayMapOrTable; //track where selection was made
var selectedWaterwayId;
function SelectWaterwayInMap(oid) { try { FeatureClickFunction(featureTypes.WATERWAY, oid); } catch (e) { HiUser(e, "Select Waterway In Map"); } }
function SelectWaterwayInTable(oid) {
  try {
    var sels = $("[id*='uxWaterwayOid']");
    var ids = "", $this, thisid, sendrId = "";
    sels.each(function () { // Iterate over items
      $this = $(this);
      thisid = $this.attr("id");
      if ($this.val() == oid) sendrId = thisid.replace("Oid", "Select");
    });
    if (sendrId !== "") {
      var sendr = GetControlByTypeAndId("input", sendrId);
      ProcessSelectWaterway(sendr);
    }
  } catch (e) { HiUser(e, "Select Waterway In Table"); }
}
function ProcessSelectWaterway(sendr) {
  try {
    var sendrId = sendr.id;
    //turn off other selections, reset css
    $('#' + sendrId).prop("checked", true);
    $('#uxWaterwayContainer input[type="radio"]:not(#' + sendrId + ')').prop("checked", false);
    $('#uxWaterwayContainer input[type="radio"]:not(#' + sendrId + ')').parent().removeClass("accord-header-highlight");
    //highlight selection
    $(sendr).parent().addClass("accord-header-highlight");

    var oid = $("#" + sendr.id.replace("Select", "Oid")).val();
    var feat = myWaterways.GetFeatureByOid(oid);

    if ("table" === waterwayMapOrTable) { GGLMAPS.event.trigger(feat.geometry, 'click', {}); }
    //    if ("table" === waterwayMapOrTable) FeatureClickListener(feat, featureTypes[0], featureGeometrys[2], oid, google.maps.event.trigger(feat, 'click'));

    selectedWaterwayId = sendrId;
    EnableTools('waterway');
    $.observable(waterwaysJson).setProperty("selectedID", GetSelectedWaterwayId());
  } catch (e) { HiUser(e, "Process Select Waterway"); }
}

function StartDrawingWaterway(sendr) {
  var okToCont = false;
  try {
    LoadSMPolygons(myWaterways, myDivides);
    var poly0 = new GGLMAPS.MVCArray();
    polyPoints = new GGLMAPS.MVCArray();
    var viewBnds = gglmap.getBounds();
    var featBnds = null;
    if (actionType === actionTypes.EDIT) {
      SetSM(waterwayStyles[1], false);
      okToCont = true;
      preEditCoords = editFeat.waterwayRecord.Coords;
      polyShape = editFeat.geometry;
      if (polyShape) {
        var pth, pthLen, gotAt;
        pth = polyShape.getPath();
        pthLen = pth.getLength();
        poly0 = new GGLMAPS.MVCArray();
        for (i = 0; i < pthLen; i++) {
          gotAt = pth.getAt(i);
          if (isNaN(gotAt.lat()) || isNaN(gotAt.lng())) continue;
          poly0.push(gotAt);
        }
        //polyPoints.push(poly0);
        polyPoints = poly0;
      }
      PrepareWaterway(waterwayStyles, 1);
      polyShape.setEditable(true);
      AddFeatureClickEvents(polyShape, featureType, featureGeometry, editingOid);
      editFeat.geometry.setMap(null);
      featBnds = myWaterways.GetExtentByOids([editingOid]);
      if (featBnds && viewBnds && !viewBnds.intersects(featBnds) && polyShape && polyShape.getPaths()) { SetMapExtentByOids(featureTypes.WATERWAY, [editingOid]); }
    } else { //not editing 
      featureInfoOk = 1; // VerifyWaterwayInfo();
      if (featureInfoOk === 1) {
        SetSM(waterwayStyles[0], false);
        okToCont = true;
        featureGeometry = featureGeometrys[1];
        RemoveClass(document.getElementById("uxCreateWaterwayDrawSubmit"), "display-none");
        polyPoints = new GGLMAPS.MVCArray(); //only 1 path for lines
        PrepareWaterway(waterwayStyles, 0);
        polyShape.setEditable(true);
      } else {
        okToCont = false;
      } // END: featureInfoOk
    } // END: editing
    SM.enable(polyShape);

    ShowToolsMainDiv(false);
  } catch (e) { HiUser(e, "Start Drawing"); }
  return okToCont;
}

function SelectWaterway(sendr, ev) {
  try {
    if (actionType) return;
    ClearTableSelections(featureTypes.WATERWAY);
    waterwayMapOrTable = "table"; //selected from table, run map selection
    infowindow.close();
    infowindow = new GGLMAPS.InfoWindow();
    //validate new selection
    var isChecked = sendr.checked;
    if (isChecked && sendr.id === selectedWaterwayId) return; //no change

    ProcessSelectWaterway(sendr);
    //stopPropagation or else radio button becomes unselected
    ev.stopPropagation();
  } catch (e) { HiUser(e, "Select Waterway"); }
}
function GetSelectedWaterwayId() {
  var retVal = "";
  try {
    if (-1 == selectedWaterwayId) return "";
    var idCtlName = selectedWaterwayId.replace("Select", "Guid");
    var idCtl = $("#" + idCtlName + "");
    if (1 > idCtl.length) return ""; //not found
    retVal = idCtl.val();
  } catch (e) { HiUser(e, "Get Selected Waterway Id"); }
  return retVal;
}

function CopyWaterway(sendr) {
  try {
    if (myWaterways.count > 0) {
      if (!editFeat || featureTypes.WATERWAY !== editingFeatType) { alert("Please select a waterway first."); return false; }
      else CopyWaterway_Part2(sendr);
    } else {
      HiUser("No waterway exists.\n\nPlease create a waterway first.");
      return;
    }
  } catch (err) { HiUser(err, "Copy Waterway"); }
}
function CopyWaterway_Part2(sendr) {
  try {
    var tmpPolyShape = editFeat.geometry;
    document.getElementById("uxCreateWaterways").click();
    document.getElementById("uxWaterwayDrawStart").click();

    if (tmpPolyShape) {
      var pth, pthLen, getAt;
      pth = tmpPolyShape.getPath();
      pthLen = pth.getLength();
      poly0 = new GGLMAPS.MVCArray();
      for (i = 0; i < pthLen; i++) {
        gotAt = pth.getAt(i);
        if (isNaN(gotAt.lat()) || isNaN(gotAt.lng())) continue;
        poly0.push(gotAt);
      }
      polyPoints = poly0;
    }
    PrepareWaterway(waterwayStyles, 1);
    polyShape.setEditable(true);
    AddFeatureClickEvents(polyShape, featureType, featureGeometry, editingOid);

  } catch (err) { HiUser(err, "Copy Waterway 2"); }
}
function EditWaterway(sendr) {
  try {
    if (myWaterways.count > 0) {
      if (!editFeat || featureTypes.WATERWAY !== editingFeatType) { alert("Please select a waterway first."); return false; }
      else EditWaterway_Part2(sendr);
    } else {
      HiUser("No waterway exists.\n\nPlease create a waterway first.");
      return;
    }
  } catch (err) { HiUser(err, "Edit Waterway"); }
}
function EditWaterway_Part2(sendr) {
  try {
    if (!editFeat || featureTypes.WATERWAY !== editingFeatType) { alert("Please select a waterway first."); return false; }
    infowindow.close();
    LoadWaterwayOptions("Edit");
    EditFeature(sendr, featureTypes.WATERWAY, editingFeatIndx, editingOid);
  } catch (err) { HiUser(err, "Edit Waterway 2"); }
}
function BeginNewWaterway(sendr) {
  try {
    BeginNewFeature(sendr);
    featureType = featureTypes.WATERWAY;
    ClearWaterwayToolsForm();
    var maxIndex = myWaterways.count;
    document.getElementById("uxCreateWaterwayOrdinal").value = maxIndex + 1;
    var lbl = document.getElementById("uxMaxWaterwayOrdinalInfo");
    lbl.innerHTML = "(max of " + (ParseInt10(maxIndex) + 1).toString() + ")";
    ShowFeatureTools(featureType, sendr);
  } catch (e) { HiUser(e, "Begin New Waterway"); }
}
function LoadWaterwayOptions(action) { }
function FinishSubmitWaterway(closeForm, action) {
  FinishSubmitFeature(closeForm, action);
  ClearWaterwaySelection();
  if (closeForm) CloseForm("ux" + action + "Waterway");
  inDrawMode = false;
}
function SubmitToDatabaseWaterway() {
  infowindow.close();
  var action, svcData, closeForm;
  var projId = GetProjId();

  var waterwayData = {};
  if (actionType === actionTypes.ADD) {
    try {
      action = "Create";
      waterwayData = GetWaterwayForWebService(action);
      if (null === waterwayData) return;

      coords = FormatLatLngCoordsForCodeBehind(polyShape);
      if (!coords || coords.trim() === "") {
        coords = "";
        HiUser("No coordinates for waterway. Please try again.");
        return;
      } else {
        coords = escape(coords); //if passing coords as string
        polyShape.setMap(null);
      }
      waterwayData["Coords"] = coords;
      waterwayData = JSON.stringify(waterwayData); // Stringify to create json object

      svcData = {};
      svcData["projectId"] = ParseInt10(projId);
      svcData["featureData"] = waterwayData;
      closeForm = true;
      ClearWaterwaySelection();
      editingFeatIndx = -1;
      SetWebServiceIndicators(true, "Submitting Waterway");
      if ("Create" === action) {
        $.ajax({
          url: "GISTools.asmx/AddWaterway"
        , data: JSON.stringify(svcData)
        })
        .done(function (data, textStatus, jqXHR) {
          waterwaysJsonD = data.d;
          if (waterwaysJsonD.info && waterwaysJsonD.info.length > 0) HiUser(waterwaysJsonD.info, "Add Waterway succeeded");
          LoadWaterwaysDone();
          myWaterways.SetFeatures();
          infowindow.close();
        })
        .fail(function (jqXHR, textStatus, errorThrown) {
          closeForm = false;
          var errorResult = errorThrown;
          HiUser(errorResult, "Create Waterway failed.");
        })
        .always(function () {
          FinishSubmitWaterway(closeForm, action);
        });
      }
    } catch (e) { HiUser(e, "Submit Waterway Add"); }
  }
  else if (actionType === actionTypes.EDIT) {
    try {
      action = "Edit";
      waterwayData = GetWaterwayForWebService(action);
      if (null === waterwayData) return;

      coords = FormatLatLngCoordsForCodeBehind(polyShape);
      if (!coords || coords.trim() === "") {
        coords = "";
        HiUser("No coordinates for waterway. Please try again.");
        return;
      } else {
        coords = escape(coords); //if passing coords as string
        polyShape.setMap(null);
      }
      waterwayData["Coords"] = coords;
      waterwayData = JSON.stringify(waterwayData); // Stringify to create json object

      svcData = {};
      svcData["projectId"] = ParseInt10(projId);
      svcData["featureId"] = editingOid;
      svcData["featureData"] = waterwayData;
      closeForm = true;
      ClearWaterwaySelection();
      editingFeatIndx = -1;
      SetWebServiceIndicators(true, "Submitting Waterway");
      if ("Edit" === action) {
        $.ajax({
          url: "GISTools.asmx/EditWaterway"
          , data: JSON.stringify(svcData)
        })
      .done(function (data, textStatus, jqXHR) {
        waterwaysJsonD = data.d;
        if (waterwaysJsonD.info && waterwaysJsonD.info.length > 0) HiUser(waterwaysJsonD.info, "Edit Waterway succeeded");
        LoadWaterwaysDone();
        myWaterways.SetFeatures();
        infowindow.close();
      })
      .fail(function (jqXHR, textStatus, errorThrown) {
        closeForm = false;
        var errorResult = errorThrown;
        HiUser(errorResult, "Edit Waterway failed.");
      })
      .always(function () {
        FinishSubmitWaterway(closeForm, action);
      });
      }
    } catch (e) { HiUser(e, "Submit Waterway Edit"); }
  }
  StopDrawing();
}

function DeleteWaterway(sendr) {
  try {
    if (myWaterways.count > 0) {
      if (!editFeat || featureTypes.WATERWAY !== editingFeatType) { alert("Please select a waterway first."); return false; }
      else DeleteWaterway_Part2(sendr);
    } else {
      HiUser("No waterway exists to delete.");
      return;
    }
  } catch (e) { HiUser(e, "Delete Waterway"); }
}
function DeleteWaterway_Part2(sendr) {
  try {
    infowindow.close();
    if ("undefined" === typeof editingFeatType || featureTypes.WATERWAY !== editingFeatType
      /*|| "undefined" === typeof editingFeatIndx || 0 > editingFeatIndx*/ || "undefined" === typeof editingOid || 0 > editingOid) { alert("Please select a waterway first"); return false; }
    if ("undefined" === typeof editingOid || editingOid < 0) { alert("Please select a waterway first."); return false; }
    var ovToDel = myWaterways.GetFeatureByOid(editingOid);

    var confMsg = CR + CR;
    confMsg += "Are you sure you want to delete this waterway?" + CR + CR + CR;
    var YorN = confirm(confMsg);
    if (YorN) {
      try {
        $("[id$=uxWaterwaysInfo]").html("");
        var projId = GetProjId();
        var svcData = "{projectId:{1},id:'{2}'}".replace("{1}", ParseInt10(projId)).replace("{2}", editingOid);
        SetWebServiceIndicators(true, "Deleting Waterway");
        $.ajax({
          url: "GISTools.asmx/DeleteWaterway"
          , data: svcData
        })
        .done(function (data, textStatus, jqXHR) {
          ovToDel.Hide();
          waterwaysJsonD = data.d;
          if (waterwaysJsonD.info && waterwaysJsonD.info.length > 0) HiUser(waterwaysJsonD.info, "Delete Waterway succeeded");
          LoadWaterwaysDone();
          myWaterways.SetFeatures();
          infowindow.close();
        })
        .fail(function (jqXHR, textStatus, errorThrown) {
          var errorResult = errorThrown;
          HiUser(errorResult, "Delete Waterway failed.");
        })
        .always(function () {
          SetWebServiceIndicators(false);
          ClearWaterwaySelection();
        });
        //    waterwaysRetrievedIndx = editingFeatIndx; //store waterway indx for reselection
      } catch (err) { HiUser(err, "Delete Waterways 2"); SetWebServiceIndicators(false); }
    }
  } catch (e) { HiUser(e, "Delete Waterway 2"); }
}
function ShowWaterwayTools(feattype, sendr) {
  try {
    var sendrId = sendr.id;
    if (sendrId.indexOf("Edit") > -1) {
      var feat = myWaterways.GetFeatureByOid(editingOid);
      var add = document.getElementById("uxEditWaterwayDrawNew");
      var mve = document.getElementById("uxEditWaterwayDrawStart");
      if (feat.waterwayRecord.Shape == "") { AddClass(mve, "display-none"); RemoveClass(add, "display-none"); }
      else { AddClass(add, "display-none"); RemoveClass(mve, "display-none"); }
    }

    inDrawMode = false;
    SetDisplayWithToolsOpen(false);
    var featdesc = "Waterway";

    var toolsObj = GetControlByTypeAndId("div", "uxCreateWaterwayContainer");
    if (actionType === actionTypes.EDIT) toolsObj = GetControlByTypeAndId("div", "uxEditWaterwayContainer");

    SetDisplayCss(toolsObj, true); // show tools div
    ShowToolsMainDiv(true); // show options part of div
    if (actionType === actionTypes.ADD) ClearToolsFormsOptions(); // clear things if adding new
    featureGeometry = featureGeometrys[1];
    //SetFormBaseLocation(toolsObj,"uxWaterwaysContainer");

    SetStartDrawingButtonText("Start Drawing", "Start drawing a new " + featdesc.toLowerCase());
    SetSubmitButtonText("Submit", "Submit the new " + featdesc.toLowerCase());

    if (actionType === actionTypes.EDIT) {
      SetStartDrawingButtonText("Start Drawing", "Draw a shape for the current feature");
      switch (feattype) {
        case featureTypes.WATERWAY:
          if (editFeat.waterwayRecord.Coords.trim().length > 0) SetStartDrawingButtonText("Edit Shape", "Edit the shape of the current feature");
          OpenForm('uxEditWaterway');
          break;
        default:
          HiUser("Feature type is not set.", "Show " + featdesc + " Tools");
          break;
      } // END: feattype check (switch)

      SetSubmitButtonVis(true);
      SetSubmitButtonText("Submit Edits", "Submit edits to database");
    }

  } catch (e) { HiUser(e, "Show Waterway Tools"); }
}
function CancelWaterwayDraw(param) {
  try { SM.disable(); } catch (e) { console.log("Disabling SM", e); }
  SMPolygons = [];
  try {
    if (actionType === actionTypes.EDIT && inDrawMode) {
      if (polyShape) polyShape.setMap(null);
      polyPoints = new GGLMAPS.MVCArray();
      if (editFeat) { SetWaterwayGeometry(editFeat, preEditCoords, editingOid); myWaterways.HighlightFeature(editingOid); }
      gglmap.setOptions({ disableDoubleClickZoom: false, draggableCursor: 'auto' });
      document.body.style.cursor = 'auto';
      cancelWaterwayDrawHandled = true;
    } else if (actionType === actionTypes.EDIT) {
      if (cancelWaterwayDrawHandled !== true) CancelDraw();
      cancelWaterwayDrawHandled = true;
    } else {
      CancelDraw();
    }

    if (HasClass(GetControlByTypeAndId('div', 'uxCreateWaterwayMain'), "display-none")) ShowToolsMainDiv(true);
    else HideWaterwayTools();

    inDrawMode = false;
    SetDisplayStartDrawingButtons(true);
    if (param === "submitted") {
      SetVisibilityCss(GetControlByTypeAndId('input', 'uxWaterwayAddNew'), true);
      SetStartDrawingButtonText("Edit Shape", "Edit the shape of the current feature");
      EditFeature(null, featureTypes.HIGHPOINT, editingFeatIndx, editingOid);
    }
    if (polyShape) polyShape.setMap(null);
    polyShape = null;
  } catch (e) { HiUser(e, "Cancel Waterway Draw"); }
}
function HideWaterwayTools() {
  SetDisplayCss(GetControlByTypeAndId("div", "uxCreateWaterwayContainer"), false);
  CloseForm('uxEditWaterway');
  HideFeatureTools();
}
function ClearWaterwayToolsForm() {
  try {
  } catch (e) { HiUser(e, "Clear Waterway Tools Form"); }
}
function GetWaterwayForWebService(action) {
  var features = {};
  var json, strCount = 0, datatypes = "";
  try {
    var features = {};    // Create empty javascript object
    var $this, thisid, attr, dataType, dte, replcVal = "ux" + action + "Waterway";
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
    json = JSON.stringify(features); // Stringify to create json object
  } catch (e) { HiUser(e, "Get Waterway For Web Service"); return null; }
  return features;
}
function ReloadWaterways() {
  try {
    $("[id$=uxWaterwaysInfo]").html("");
    SetWebServiceIndicators(true, "Getting waterways");
    var projId = GetProjId();
    var svcData = "{projectId:{0}}".replace("{0}", ParseInt10(projId));
    $.ajax({
      url: "GISTools.asmx/GetWaterways"
      , data: svcData
    })
    .done(function (data, textStatus, jqXHR) {
      waterwaysJsonD = data.d;
      if (waterwaysJsonD.info && waterwaysJsonD.info.length > 0) HiUser(waterwaysJsonD.info, "Get Waterways succeeded");
      LoadWaterwaysDone();
      myWaterways.SetFeatures();
    })
    .fail(function (jqXHR, textStatus, errorThrown) {
      var errorResult = errorThrown;
      HiUser(errorResult, "Get Waterways failed.");
    })
    .always(function () {
      SetWebServiceIndicators(false);
      ClearWaterwaySelection();
    });
    //    waterwaysRetrievedIndx = editingFeatIndx; //store waterway indx for reselection
  } catch (err) { HiUser(err, "Load Waterways"); SetWebServiceIndicators(false); }
}
function LoadWaterwaysDone() {
  try {
    var info = "";
    if (!waterwaysJsonD || !waterwaysJsonD.waterways || waterwaysJsonD.waterways.length === 0) info = "You do not have any waterways created. Use the tools button to create a new waterway.";

    RenderWaterways();
    $("[id$=uxWaterwaysInfo]").html(info); //set after linking or DNE
  } catch (e) { HiUser(e, "Load Waterways Done"); }
}
function RenderWaterways() {
  try {
    if (!waterwaysJsonD || !waterwaysJsonD.waterways || waterwaysJsonD.waterways.length === 0) {
      waterwaysJson = {};
      return;
    }
    var waterwaysJsonWaterways = waterwaysJsonD.waterways;

    waterwaysJson = {
      waterways: waterwaysJsonWaterways
      , selectedID: (waterwaysJsonWaterways && waterwaysJsonWaterways.length > 0) ? waterwaysJsonWaterways[0].datumRecord.GUID : '0'
      , selected: function () {
        try {
          for (var i = 0; i < waterwaysJsonWaterways.length; i++) {
            if (waterwaysJsonWaterways[i].datumRecord.GUID === this.selectedID) {
              return waterwaysJsonWaterways[i];
            }
          }
        } catch (e) { HiUser(e, "Show Waterways selected"); }
        return {};
      }
    };
    FleshOutWaterways();

    waterwaysJson.selected.depends = "selectedID";

    waterwaysTmpl.link("#uxWaterwayContainer", waterwaysJson);
    editWaterwaysTmpl.link("#uxEditWaterwayContainer", waterwaysJson);
    SetAccordions();
    $('input:radio[name*="WaterwaySelect"]').off('click').on('click', function (e) { SelectWaterway(this, e); });
  } catch (e) { HiUser(e, "Render Waterways"); }
}
function FleshOutWaterways() {
  try {
    var feats = waterwaysJson.waterways;
    var feat, featRec;
    for (var f in feats) {
      feat = feats[f];
      featRec = feat.waterwayRecord;
      if (!featRec) continue;
      feat.geometry = new GGLMAPS.Polyline();
      SetWaterwayGeometry(feat, featRec.Coords, featRec.ObjectID);
      feat.Show = function () { this.geometry.setMap(gglmap); };
      feat.Hide = function () { this.geometry.setMap(null); };
    }
  } catch (e) { HiUser(e, "Flesh Out Waterways"); }
}
function SetWaterwayGeometry(feat, featCoords, featOid) {
  try {
    polyPoints = CreateMvcPointArray(featCoords);
    PrepareWaterway(waterwayStyles, 0);
    feat.geometry = polyShape;
    feat.geometry.parent = featOid;
    feat.geometry.bounds = GetBoundsForPoly(polyShape);
    feat.geometry.center = GetCenterOfCoordsString(featCoords);
    AddFeatureClickEvents(feat.geometry, featureTypes.WATERWAY, featureGeometrys[2], featOid);
    ClearDrawingEntities();
  } catch (e) { HiUser(e, "Set Waterway Geometry"); }
}

function OpenOrderWaterwayTool(sendr) {
  try {
    var warning = document.getElementById("uxOrderWaterwayWarning");
    warning.innerHTML = "";
    SetDisplayWithToolsOpen(false);

    var toolsObj = GetControlByTypeAndId("div", "uxOrderWaterwayContainer");
    SetDisplayCss(toolsObj, true); // show tools div
    //SetFormBaseLocation(toolsObj,"uxWaterwaysContainer");

    var lst = document.getElementById("uxOrderWaterway" + "List");
    lst.innerHTML = "";

    var feat;
    var sorted = myWaterways.Sort("Ordinal", false);
    for (var sortIx = 0; sortIx < sorted.length; sortIx++) {
      feat = sorted[sortIx];
      AddOrderWaterwayRow("uxOrderWaterway", feat);
    }

  } catch (e) { HiUser(e, "Open Ordering Tool"); return null; }
}
function CancelOrderWaterwayTool() {
  CloseForm("uxOrderWaterway");
  SetDisplayWithToolsOpen(true);
}
function DecrementWaterwayIndex(ctlName) { //can't go below 1
  var ctl = document.getElementById(ctlName);
  var curr = ParseInt10(ctl.value);
  var min = 1;
  if (curr > min) {
    curr -= 1;
    ctl.value = curr;
  }
}
function IncrementWaterwayIndex(ctlName) { //can go to max+1
  var ctl = document.getElementById(ctlName);
  var curr = ParseInt10(ctl.value);
  var max = myWaterways.count;
  if (curr <= max) {
    curr += 1;
    ctl.value = curr;
  }
}
function SwapWaterwayIndex(sendr, delta) {
  var max = myWaterways.count;
  var indexCtl = $(sendr.parentNode).find("[data-field='Ordinal']")[0];
  var currVal = ParseInt10(indexCtl.value);

  if (delta < 0 && currVal == 1) return;
  if (delta > 0 && currVal == max) return;
  var seekVal = currVal + delta;

  var list = $("#uxOrderWaterwayList");
  var items = list.find("li");
  var itemIndexCtl;
  items.each(function () {
    try {
      itemIndexCtl = $(this).find("[data-field='Ordinal']");
      if (itemIndexCtl.val() == seekVal) {
        itemIndexCtl.val(currVal);
        return false;
      }
    } catch (e) { HiUser(e, "SwapWaterwayIndex"); }
  });
  $(indexCtl).val(seekVal);
}
function AddOrderWaterwayRow(lstName, feat) {
  try {
    var lst = document.getElementById(lstName + "List");
    var cel, ctl, val;

    //<li>
    var rw = document.createElement("li");

    //  <input type="hidden" data-field="4249" />
    ctl = document.createElement("input");
    ctl.setAttribute("data-field", "ObjectID");
    ctl.setAttribute("type", "hidden");
    val = "";
    if (feat) { val = feat.datumRecord.ObjectID; }
    ctl.setAttribute("value", val);
    rw.appendChild(ctl);

    //  <span class="left-column">
    var oid = feat.datumRecord.ObjectID;
    cel = document.createElement("span");
    cel.setAttribute("class", "left-column");
    //    <input type="button" onclick="myWaterways.HighlightFeature(4249);" value="Highlight" />
    ctl = document.createElement("input");
    ctl.setAttribute("type", "button");
    ctl.setAttribute("title", "Highlight feature on map");
    ctl.setAttribute("value", "Highlight");
    ctl.onclick = function () { myWaterways.HighlightFeature(oid); };
    cel.appendChild(ctl);
    rw.appendChild(cel);
    //  </span>

    //  <span class="right-side">
    cel = document.createElement("span");
    cel.setAttribute("class", "right-side");
    //    <input type="button" class="arrow-button" value="<" onclick="SwapWaterwayIndex(this, -1);"
    //      title="Click to decrement the index for this feature." />
    ctl = document.createElement("input");
    ctl.setAttribute("type", "button");
    ctl.setAttribute("class", "arrow-button");
    ctl.setAttribute("title", "Click to decrement the index for this feature.");
    ctl.setAttribute("value", "<");
    ctl.onclick = function () { SwapWaterwayIndex(this, -1); };
    cel.appendChild(ctl);
    //    <input type="text" data-type="text" data-field="Ordinal" value="2" />
    ctl = document.createElement("input");
    ctl.setAttribute("type", "text");
    ctl.setAttribute("class", "text-center");
    ctl.setAttribute("data-type", "text");
    ctl.setAttribute("data-field", "Ordinal");
    ctl.setAttribute("value", feat.waterwayRecord.Ordinal);
    cel.appendChild(ctl);
    //    <input type="button" class="arrow-button" value=">" onclick="SwapWaterwayIndex(this, 1);"
    //      title="Click to increment the index for this feature." />
    ctl = document.createElement("input");
    ctl.setAttribute("type", "button");
    ctl.setAttribute("class", "arrow-button");
    ctl.setAttribute("title", "Click to increment the index for this feature.");
    ctl.setAttribute("value", ">");
    ctl.onclick = function () { SwapWaterwayIndex(this, 1); };
    cel.appendChild(ctl);
    //  </span>
    //</li>

    rw.appendChild(cel);
    lst.appendChild(rw);
  } catch (e) { HiUser(e, "Add Ordering Row"); }
}
function SubmitOrderWaterwayTool() {
  var warning = document.getElementById("uxOrderWaterwayWarning");
  warning.innerHTML = "";
  var lst = document.getElementById("uxOrderWaterway" + "List");
  var rows = lst.getElementsByTagName("li");
  var max = myWaterways.count;
  var ordinals = [];
  var featData = [];
  var rowData = {};
  var missingData = false;
  var inputs, ord;
  $(rows).each(function () {
    try {
      $this = $(this);
      inputs = $this.find("input");
      rowData = {};
      rowData["ObjectID"] = inputs.filter("[data-field='ObjectID']").val();
      ord = inputs.filter("[data-field='Ordinal']").val();
      rowData["Ordinal"] = ord;
      ordinals.push(ParseInt10(ord));
      featData.push(rowData);
    } catch (e) { HiUser(e, "SubmitOrderingToolWaterway"); }
  });

  for (var ordIx = 1; ordIx <= max; ordIx++) {
    if (ordinals.indexOf(ordIx) < 0) { missingData = true; warning.innerHTML += "<br />Missing index " + ordIx; }
  }

  if (!missingData) FinishSubmitOrderWaterwayTool(featData);
}
function FinishSubmitOrderWaterwayTool(featData) {
  try {
    var projId = GetProjId();
    var svcData = {};
    svcData["featureData"] = JSON.stringify(featData);
    svcData["projectId"] = ParseInt10(projId);

    SetWebServiceIndicators(true, "Submitting Waterway Indexing");
    $.ajax({
      url: "GISTools.asmx/UpdateWaterwayOrdering"
    , data: JSON.stringify(svcData)
    })
    .done(function (data, textStatus, jqXHR) {
      waterwaysJsonD = data.d;
      if (waterwaysJsonD.info && waterwaysJsonD.info.length > 0) HiUser(waterwaysJsonD.info, "Update Waterways succeeded");
      LoadWaterwaysDone();
      myWaterways.SetFeatures();
    })
    .fail(function (jqXHR, textStatus, errorThrown) {
      var errorResult = errorThrown;
      HiUser(errorResult, "Update Waterway failed.");
    })
    .always(function () {
      //FinishSubmitWaterway(closeForm, action);
      SetWebServiceIndicators(false);
    });
  } catch (e) { HiUser(e, "Submit Waterway Indexing"); }
}

var firstPointWaterways = [];
function OpenAlignWaterwayTool(sendr) {
  try {
    var warning = document.getElementById("uxAlignWaterwayWarning");
    warning.innerHTML = "";
    SetDisplayWithToolsOpen(false);

    var toolsObj = GetControlByTypeAndId("div", "uxAlignWaterwayContainer");
    SetDisplayCss(toolsObj, true); // show tools div
    //SetFormBaseLocation(toolsObj,"uxWaterwaysContainer");

    var lst = document.getElementById("uxAlignWaterway" + "List");
    lst.innerHTML = "";
     
    SetAlignWaterwayVisuals(true);
  } catch (e) { HiUser(e, "Open Aligning Tool"); return null; }
}
function CancelAlignWaterwayTool() {
  ClearFirstPointWaterways();
  CloseForm("uxAlignWaterway");
  SetDisplayWithToolsOpen(true);
}
function SetAlignWaterwayVisuals(addRow) {
  try {
    var feat;
    var sorted = myWaterways.Sort("Ordinal", false);
     
    ClearFirstPointWaterways();
    for (var sortIx = 0; sortIx < sorted.length; sortIx++) {
      feat = sorted[sortIx];
      ShowFirstPointWaterway(feat);
      if (addRow) AddAlignWaterwayRow("uxAlignWaterway", feat);
    }
  } catch (e) { HiUser(e, "Set Align Waterways"); }
}
function ShowFirstPointWaterway(feat) {
  try{
    var coords = feat.waterwayRecord.Coords;
    var firstCoord = coords.split(" ")[0];
    if (firstCoord.trim() == "") return;
    var ords = firstCoord.split(",");
    var lng = ords[0];
    var lat = ords[1];
    var ll = new google.maps.LatLng(lat, lng);
    var marker = new google.maps.Marker();
    marker.setMap(gglmap);
    marker.setPosition(ll);
    firstPointWaterways.push(marker);
  } catch (e) { HiUser(e, "Show First Point Waterway"); }
}
function ClearFirstPointWaterways() {
  try {
    for (var markerIx = 0; markerIx < firstPointWaterways.length; markerIx++) {
      firstPointWaterways[markerIx].setMap(null);
    }
    firstPointWaterways = [];
  } catch (e) { HiUser(e, "Show First Point Waterway"); }
}
function AddAlignWaterwayRow(lstName, feat) {
  try {
    var lst = document.getElementById(lstName + "List");
    var cel, ctl, val;

    //<li>
    var rw = document.createElement("li");

    //  <input type="hidden" data-field="4249" />
    ctl = document.createElement("input");
    ctl.setAttribute("data-field", "ObjectID");
    ctl.setAttribute("type", "hidden");
    val = "";
    if (feat) { val = feat.datumRecord.ObjectID; }
    ctl.setAttribute("value", val);
    rw.appendChild(ctl);

    //  <span class="left-column">
    var oid = feat.datumRecord.ObjectID;
    cel = document.createElement("span");
    cel.setAttribute("class", "left-column");
    //    <input type="button" onclick="myWaterways.HighlightFeature(4249);" value="Highlight" />
    ctl = document.createElement("input");
    ctl.setAttribute("type", "button");
    ctl.setAttribute("title", "Highlight feature on map");
    ctl.setAttribute("value", "Highlight");
    ctl.onclick = function () { myWaterways.HighlightFeature(oid); };
    cel.appendChild(ctl);
    rw.appendChild(cel);
    //  </span>

    //  <span class="right-side">
    cel = document.createElement("span");
    cel.setAttribute("class", "right-side");
    //<input type="button" value="Reverse" onclick="ReverseWaterway(this);"
    //  title="Click to reverse the coordinate order for this feature." />
    ctl = document.createElement("input");
    ctl.setAttribute("type", "button");
    ctl.setAttribute("title", "Click to reverse the coordinate order for this feature.");
    ctl.setAttribute("value", "Reverse");
    ctl.onclick = function () { ReverseWaterway(oid); };
    cel.appendChild(ctl);
    //<label data-warning="geometry" class="display-none">No geometry</label>
    ctl = document.createElement("label");
    ctl.setAttribute("data-warning", "geometry");
    var feat = myWaterways.GetFeatureByOid(oid);
    var coords = feat.waterwayRecord.Coords;
    ctl.setAttribute("value", "Reverse");
    if (coords.trim() != "") ctl.setAttribute("class", "display-none");
    ctl.onclick = function () { ReverseWaterway(oid); };
    cel.appendChild(ctl);
    //<input type="hidden" data-field="Coords" value="3,4 5,9" />
    ctl = document.createElement("input");
    ctl.setAttribute("data-field", "Coords");
    ctl.setAttribute("type", "hidden");
    val = "";
    if (feat) { val = feat.waterwayRecord.Coords; }
    ctl.setAttribute("value", val);
    cel.appendChild(ctl);
    //  </span>
    //</li>

    rw.appendChild(cel);
    lst.appendChild(rw);
  } catch (e) { HiUser(e, "Add Ordering Row"); }
}
function ReverseWaterway(featId) {
  try {
    var feat = myWaterways.GetFeatureByOid(featId);
    var coords = feat.waterwayRecord.Coords.split(" ");
    var newCoords = [];
    var len = coords.length;
    if (len < 1) return;
    for (var coordIx = len - 1; coordIx >= 0; coordIx--) {
      newCoords.push(coords[coordIx]);
    }
    feat.waterwayRecord.Coords = newCoords.join(" ");
    SetAlignWaterwayVisuals(false);
  } catch (e) { HiUser(e, "Reverse Waterway"); }
}
function SubmitAlignWaterwayTool() {
  var warning = document.getElementById("uxAlignWaterwayWarning");
  warning.innerHTML = "";
  var lst = document.getElementById("uxAlignWaterway" + "List");
  var rows = lst.getElementsByTagName("li");
  var missingData = false;
  var featData = [];
  var rowData = {};
  var inputs, feat, coords;
  $(rows).each(function () {
    try {
      $this = $(this);
      inputs = $this.find("input");
      rowData = {};
      rowData["ObjectID"] = inputs.filter("[data-field='ObjectID']").val();
      feat = myWaterways.GetFeatureByOid(rowData["ObjectID"]);
      coords = feat.waterwayRecord.Coords;
      //if (coords.trim() == "") { missingData = true; }
      rowData["Coords"] = coords;
      featData.push(rowData);
    } catch (e) { HiUser(e, "SubmitAligningToolWaterway"); }
  });

  FinishSubmitAlignWaterwayTool(featData);
}
function FinishSubmitAlignWaterwayTool(featData) {
  try {
    var projId = GetProjId();
    var svcData = {};
    svcData["featureData"] = JSON.stringify(featData);
    svcData["projectId"] = ParseInt10(projId);

    SetWebServiceIndicators(true, "Submitting Waterway Align");
    $.ajax({
      url: "GISTools.asmx/UpdateWaterwayAligning"
    , data: JSON.stringify(svcData)
    })
    .done(function (data, textStatus, jqXHR) {
      waterwaysJsonD = data.d;
      if (waterwaysJsonD.info && waterwaysJsonD.info.length > 0) HiUser(waterwaysJsonD.info, "Update Waterways succeeded");
      LoadWaterwaysDone();
      myWaterways.SetFeatures();
    })
    .fail(function (jqXHR, textStatus, errorThrown) {
      var errorResult = errorThrown;
      HiUser(errorResult, "Update Waterway failed.");
    })
    .always(function () {
      //FinishSubmitWaterway(closeForm, action);
      SetWebServiceIndicators(false);
      CancelAlignWaterwayTool(); //force reopen to update Coords hidden field
    });
  } catch (e) { HiUser(e, "Submit Waterway Aligning"); }
}