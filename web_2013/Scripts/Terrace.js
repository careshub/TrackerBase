/// <reference path="http://ajax.aspnetcdn.com/ajax/jQuery/jquery-1.5-vsdoc.js"/>
/// <reference path="Site.js" />
//FEATUREID	PFEATURES	  	      LABELDESC	          UNITMEASURE		MARKERSYM	 	MARKERSYMHEX	DRAWORDER
//40	    CONVENTIONAL TERRACE	LENGTH IN FEET	    FEET		      255,255,0	 	FFFF00	      NULL
//45	    CUSTOM TERRACE 	  	  LENGTH IN FEET	    FEET		      255,255,0	 	FFFF00	      NULL
//36	    KEY TERRACE	  	      LENGTH IN FEET	    FEET		      122,139,139	2F4F00	      NULL
//37	    PARALLEL TERRACE	  	LENGTH IN FEET	    FEET		      255,0,0	 	  FF0000	      NULL
//41	    TERRACE AREA	    	  AREA IN ACRES	      ACRES		      255,215,0 	ffd700	      2

var terraceCategorys = ["Smooth", "Original", "Filled"];
var terraceType; // holds value from actionTypes
var terraceTypes = {
  CONVENTIONAL: 1, CUSTOM: 2, KEY: 3, PARALLEL: 4,
  properties: {
    1: { value: 1, name: "Conventional Terrace", code: 40, color: "#FFFF00", selColor: "#77F7E0" /*"#0000FF"*/ },
    2: { value: 2, name: "Custom Terrace", code: 45, color: "#FFFF00", selColor: "#77F7E0" /*"#0000FF"*/ },
    3: { value: 3, name: "Key Terrace", code: 36, color: "#2F4F00", selColor: "#77F7E0" /*"#D0B0FF"*/ },
    4: { value: 4, name: "Parallel Terrace", code: 37, color: "#FF0000", selColor: "#77F7E0" /*"#00FFFF"*/ }
  }
};
if (Object.freeze) Object.freeze(terraceTypes);

myTerraces = {
  count: 0
  , cls: 'Terrace'
  , heading: 'Terrace'
  , color: "#FFFF00"
  , selColor: "#77F7E0" // "#0000FF"
  , features: {}
  , featureLabels: []
  , Init: function () {
    try { this.SetFeatures(); } catch (e) { HiUser(e, "Init Terraces"); }
  }
  , Reset: function () {
    try {
      this.Hide();
      this.count = 0;
      this.features = {};
    } catch (e) { HiUser(e, "Reset Terraces"); }
  }
  , SetFeatures: function () {
    try {
      this.Hide();
      if (!terracesJson || !(terracesJson.terraces)) { this.features = {}; this.count = 0; return; }
      this.features = terracesJson.terraces;
      this.count = this.features.length;
      this.Show();
    } catch (e) { HiUser(e, "Set Terraces"); }
  }
  , GetFeatureName: function (oid) {
    try {
      var feat = this.GetFeatureByOid(oid);
      var featRec = feat.terraceRecord;
      var name = featRec.TerraceName;
      return name;
    } catch (e) { HiUser(e, "Get Terrace Name"); }
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
    } catch (err) { HiUser(err, "Get Terrace By Guid"); }
    return null;
  }
  , GetFeatureByOid: function (oid) {
    if (!oid || oid.length === 0) return null;
    oid = ParseInt10(oid);
    var feats = this.features;
    var obj, featRec;
    try {
      for (var feat in feats) {
        obj = feats[feat];
        if (obj.terraceRecord) {
          featRec = feats[feat].terraceRecord;
          if (featRec && oid == featRec.ObjectID) return feats[feat];
        }
      }
    } catch (err) { HiUser(err, "Get Terrace By Oid"); } 
    return null;
  }
  , GetFeatureByName: function (terraceName) {
    if (!terraceName) return null;
    var feats = this.features;
    var featRec, featName;
    try {
      for (var feat in feats) {
        featRec = feats[feat].terraceRecord;
        if (featRec) {
          featName = featRec.TerraceName.toString().trim();
          if (featName == terraceName) return feats[feat];
        }
      }
    } catch (err) { HiUser(err, "Get Terrace By Name"); }
    return null;
  }
  , GetFeaturesByType: function (type) {
    if (!type) return null;
    var retVal = {};
    var countr = 0;
    var feats = this.features;
    var featRec, featType;
    try {
      for (var feat in feats) {
        featRec = feats[feat].terraceRecord;
        if (featRec) {
          featType = featRec.Type.toString().trim().toLowerCase();
          if (featType == type.toLowerCase()) retVal[countr] = feat;
        }
      }
    } catch (err) { HiUser(err, "Get Terraces By Type"); }
    return retVal;
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
        featRec = feats[feat].terraceRecord;
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
    } catch (err) { HiUser(err, "Get Terraces Extent"); }
    if (haveAFeature) { retVal = newBounds; }
    return retVal;
  }
  , GetInfoWindow: function (oid) {
    var html;
    try {
      var feat = this.GetFeatureByOid(oid);
      var featRec = feat.terraceRecord;
      html = "<div class='infoWin" + GetFeatureType(featureTypes.TERRACE) + "' id='" + GetFeatureType(featureTypes.TERRACE) + oid + "info'>";
      html += "<table class='" + this.cls.toLowerCase() + "Info' id='" + this.cls.toLowerCase() + oid + "'>";
      html += "<tr><th colspan='2' " +
        " style='background-color: " + this.selColor + ";' " +
        ">" + feat.propSet.name /*this.heading*/ + "</th></tr>";

      var currData;
      currData = (featRec.Length * ftPerMtr).toFixed(1);
      html += "<tr><td class='first'>" + "Length (ft)" + ": </td>";
      html += "<td>" + unescape(currData);
      html += "</td></tr>";
      currData = (featRec.Type);
      html += "<tr><td>" + "Type" + ": </td>";
      html += "<td>" + unescape(currData);
      html += "</td></tr>";
      currData = (featRec.Ordinal);
      html += "<tr><td>" + "Index" + ": </td>";
      html += "<td>" + unescape(currData);
      html += "</td></tr>";

      html += "</table></div>";

    } catch (err) { HiUser(err, "Get Terrace Info Window"); }
    return { content: html, position: feat.geometry.center };
  }
  , HighlightFeature: function (oid) {
    try {
      var feats = this.features;
      var featRec, featGeom;
      try {
        for (var featx in feats) {
          var feat = feats[featx];
          featRec = feat.terraceRecord, featGeom = feat.geometry;
          if (!featRec) continue;
          var featOid = featRec.ObjectID;
          if (featOid.toString() != oid.toString() &&
                  featGeom.strokeColor.toLowerCase() === feat.propSet.selColor.toLowerCase()) {
            featGeom.setOptions({ strokeColor: feat.propSet.color, zIndex: terraceZIndex });
          }
        }
      } catch (err) { HiUser(err, "Dehighlight Terrace"); }
      var feat0 = this.GetFeatureByOid(oid);
      featGeom = feat0.geometry;
      if (featGeom.getMap() == null) feat0.Show();
      featGeom.setOptions({ strokeColor: feat0.propSet.selColor, zIndex: terraceZIndex + 1 });
    } catch (err) { HiUser(err, "Highlight Terrace"); }
  }
  , RemoveHighlights: function (oid) {
    var feats = this.features;
    var feat;
    var featGeom;
    try {
      for (var featIx in feats) {
        feat = feats[featIx];
        if (oid && feat.terraceRecord && feat.terraceRecord.ObjectID != oid) continue;
        featGeom = feat.geometry;
        if (featGeom && feat.propSet) featGeom.setOptions({ strokeColor: feat.propSet.color, zIndex: terraceZIndex });
      }
    } catch (err) { HiUser(err, this.heading + " Remove Highlights"); }
  }
  , ToggleHighlight: function (oid, tOrF) {
    try {
      if (!oid) return;
      var feat = this.GetFeatureByOid(oid);
      if (arguments.length > 1) {
        if (tOrF) this.HighlightFeature(oid);
        else this.RemoveHighlights(oid);
      } else {
        var featGeom = feat.geometry;
        if (featGeom.strokeColor.toLowerCase() === feat.propSet.selColor.toLowerCase()) {
          this.RemoveHighlights(oid);
        } else {
          this.HighlightFeature(oid);
        }
      }
    } catch (err) { HiUser(err, this.heading + " Toggle Highlight"); }
  }
  , Toggle: function (oid, tOrF) {
    try {
      if (!oid) return;
      var feat = this.GetFeatureByOid(oid);
      if (arguments.length > 1) {
        if (tOrF) feat.Show();
        else feat.Hide();
      } else {
        if (feat.geometry.getMap() != null) feat.Hide();
        else feat.Show();
      }
    } catch (err) { HiUser(err, this.heading + " Toggle"); }
  }
  , Hide: function (oid) {
    try {
      if (oid) { this.GetFeatureByOid(oid).Hide(); }
      else {
        var feats = this.features;
        for (var feat in feats) if (feats[feat] && typeof feats[feat].Hide === 'function') feats[feat].Hide();
      }
    } catch (err) { HiUser(err, this.heading + " Hide"); }
  }
  , Show: function (oid) {
    try {
      if (oid) { this.GetFeatureByOid(oid).Show(); }
      else {
        var feats = this.features;
        for (var feat in feats) if (feats[feat] && typeof feats[feat].Show === 'function') feats[feat].Show();
      }
    } catch (err) { HiUser(err, this.heading + " Show"); }
  }
  , GetLabel: function (oid) {
    var retVal = "not found";
    try {
      var feat = this.GetFeatureByOid(oid);
      var featRec = feat.terraceRecord;
      var name;
      name = feat.propSet.name;
      retVal = name.toString();
    } catch (err) { HiUser(err, "Get Terrace Label"); }
    return retVal;
  }
  , ToggleLabel: function (sendr) {
    try {
      if (sendr.checked) this.ShowLabel();
      else this.HideLabel();
    } catch (err) { HiUser(err, "Toggle Terrace Label"); }
  }
  , HideLabel: function (sendr) {
    try {
      var lblsLen = this.featureLabels.length;
      for (var lblIdx = 0; lblIdx < lblsLen; lblIdx++) { this.featureLabels[lblIdx].hide(); }
    } catch (err) { HiUser(err, "Hide Terrace Labels"); }
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
        featRec = feats[feat].terraceRecord;
        if (featRec) {
          labelPos = feats[feat].geometry.center;
          if (!labelPos) continue;
          name = "ID: " + feats[feat].propSet.name;
          name += "<br />" + "Acres: " + featRec.TotalArea;
          name += "<br />" + "Spr.: " + featRec.SpreadableArea;
          labelText = name;
          myOptions = {
            content: labelText
          , boxStyle: {
            border: "3px solid black"
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
    } catch (e) { HiUser(e, "Show Terrace Labels"); }
  }
  , GetMaxOrdinal: function () {
    var retVal = 0;
    var feats = this.features;
    var featRec, featIndex;
    try {
      for (var feat in feats) {
        featRec = feats[feat].terraceRecord;
        if (featRec) {
          featIndex = ParseInt10(featRec.Ordinal);
          if (featIndex > retVal) retVal = featIndex;
        }
      }
    } catch (e) { HiUser(e, "Get Max Ordinal Terrace"); }
    return retVal;
  }
  , Sort: function (key, descTorF, sortFeats) {
    var retVal = [];
    try {
      var cnt = 0;
      var feats = sortFeats || this.features;
      for (var prop in feats) {
        if (feats.hasOwnProperty(prop) && "terraceRecord" in feats[prop]) {
          cnt++; retVal.push(feats[prop]);
        }
      }
      retVal.sort(function (a, b) { //sort
        var retVal = a["terraceRecord"][key] > b["terraceRecord"][key];
        if (descTorF) retVal = retVal * -1;
        return retVal;
      });
    } catch (err) { HiUser(err, "Sort Terraces"); }
    return retVal;
  }
}

var terracesJson, terracesJsonD;
var terraceStrokeColor = myTerraces.color;
var terraceStrokeWeight = 4;
var terraceStrokeOpacity = 1.0;
var terraceZIndex = 19;
var terraceStrokeHighlight = myTerraces.selColor;

var cancelTerraceDrawHandled = false;

var terraceStyles = [];

function TerraceStyle(name, color, width, lineopac, zindex) {
  this.name = name || "Terrace";
  this.color = color || terraceStrokeColor;
  this.width = width || terraceStrokeWeight;
  this.lineopac = lineopac || terraceStrokeOpacity;
  this.zindex = zindex || terraceZIndex;
}
function CreateTerraceStyleObject() {
  //put highlight at index 0
  var linestyle = new TerraceStyle();
  var tmpStrokeColor = terraceStrokeColor;
  terraceStrokeColor = terraceStrokeHighlight;
  linestyle = new TerraceStyle(); terraceStyles.push(linestyle);
  terraceStrokeColor = tmpStrokeColor;
  var count = GetOwnPropLength(terraceTypes.properties);

  var propSet;
  for (var propIx = 1; propIx <= count; propIx++) {
    propSet = terraceTypes.properties[propIx];
    linestyle = new TerraceStyle(null, propSet.color);
    terraceStyles.push(linestyle);
  }
}
function PrepareTerrace(styleArray, styleIndx) {
  try {
    if (!styleArray) styleArray = terraceStyles;
    if (!styleIndx) styleIndx = 0;
    var polyOptions = {
      path: polyPoints
    , strokeColor: styleArray[styleIndx].color
    , strokeOpacity: styleArray[styleIndx].lineopac
    , strokeWeight: styleArray[styleIndx].width
    , zIndex: styleArray[styleIndx].zindex
    };
    polyShape = new GGLMAPS.Polyline(polyOptions);
    polyShape.setMap(gglmap);
  } catch (e) { HiUser(e, "Prepare Terrace"); }
}

function InitializeTerraces() {
  CreateTerraceStyleObject();
  if (terracesJson) {
    terracesJsonD = terracesJson.d; //set as if web service call
    LoadTerracesDone();
    myTerraces.Init();
    var sect = document.getElementById("uxTerraceExistsStuff");
    if (ParseInt10(myTerraces.count) > 0) { RemoveClass(sect, "display-none"); }
    else { AddClass(sect, "display-none"); }
    myTerraces.Hide();
    InitToggle();
    LoadCostShares();
    SetCostShares();
    SetCustom();
  }
}

function ClearTerraceSelection(params) {
  try {
    myTerraces.RemoveHighlights();
    ClearEditFeat();
  } catch (e) { HiUser(e, "Clear Terrace Selection"); }
}
var terraceMapOrTable; //track where selection was made
var selectedTerraceId;
function SelectTerraceInMap(oid) { try { FeatureClickFunction(featureTypes.TERRACE, oid); } catch (e) { HiUser(e, "Select Terrace In Map"); } }
function SelectTerraceInTable(oid) {
  try {
    var sels = $("[id*='uxTerraceOid']");
    var ids = "", $this, thisid, sendrId = "";
    sels.each(function () { // Iterate over items
      $this = $(this);
      thisid = $this.attr("id");
      if ($this.val() == oid) sendrId = thisid.replace("Oid", "Select");
    });
    if (sendrId !== "") {
      var sendr = GetControlByTypeAndId("input", sendrId);
      ProcessSelectTerrace(sendr);
    }
  } catch (e) { HiUser(e, "Select Terrace In Table"); }
}
function ProcessSelectTerrace(sendr) {
  try {
    var sendrId = sendr.id;
    //turn off other selections, reset css
    $('#' + sendrId).prop("checked", true);
    $('#uxTerraceContainer input[type="radio"]:not(#' + sendrId + ')').prop("checked", false);
    $('#uxTerraceContainer input[type="radio"]:not(#' + sendrId + ')').parent().removeClass("accord-header-highlight");
    //highlight selection
    $(sendr).parent().addClass("accord-header-highlight");

    var oid = $("#" + sendr.id.replace("Select", "Oid")).val();
    var feat = myTerraces.GetFeatureByOid(oid);

    if ("table" === terraceMapOrTable) { GGLMAPS.event.trigger(feat.geometry, 'click', {}); }
    //    if ("table" === terraceMapOrTable) FeatureClickListener(feat, featureTypes[0], featureGeometrys[2], oid, google.maps.event.trigger(feat, 'click'));

    selectedTerraceId = sendrId;
    EnableTools('terrace');
    $.observable(terracesJson).setProperty("selectedID", GetSelectedTerraceId());
  } catch (e) { HiUser(e, "Process Select Terrace"); }
}

function SelectTerrace(sendr, ev) {
  try {
    if (actionType) return;
    ClearTableSelections(featureTypes.TERRACE);
    terraceMapOrTable = "table"; //selected from table, run map selection
    infowindow.close();
    infowindow = new GGLMAPS.InfoWindow();
    //validate new selection
    var isChecked = sendr.checked;
    if (isChecked && sendr.id === selectedTerraceId) return; //no change

    ProcessSelectTerrace(sendr);
    //stopPropagation or else radio button becomes unselected
    ev.stopPropagation();
  } catch (e) { HiUser(e, "Select Terrace"); }
}
function GetSelectedTerraceId() {
  var retVal = "";
  try {
    if (-1 == selectedTerraceId) return "";
    var idCtlName = selectedTerraceId.replace("Select", "Guid");
    var idCtl = $("#" + idCtlName + "");
    if (1 > idCtl.length) return ""; //not found
    retVal = idCtl.val();
  } catch (e) { HiUser(e, "Get Selected Terrace Id"); }
  return retVal;
}

function ClearTerraceToolsForm() {
  try {
  } catch (e) { HiUser(e, "Clear Terrace Tools Form"); }
}
function GetTerraceForWebService(action) {
  var features = {};
  var json, strCount = 0, datatypes = "";
  try {
    var features = {};    // Create empty javascript object
    var $this, thisid, attr, dataType, dte, replcVal = "ux" + action + "Terrace";
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
  } catch (e) { HiUser(e, "Get Terrace For Web Service"); return null; }
  return features;
}
function ReloadTerraces() {
  try {
    $("[id$=uxTerracesInfo]").html("");
    SetWebServiceIndicators(true, "Getting terraces");
    var projId = GetProjId();
    var svcData = "{projectId:{0}}".replace("{0}", ParseInt10(projId));
    $.ajax({
      url: "GISTools.asmx/GetTerraces"
      , data: svcData
    })
    .done(function (data, textStatus, jqXHR) {
      terracesJsonD = data.d;
      if (terracesJsonD.info && terracesJsonD.info.length > 0) HiUser(terracesJsonD.info, "Get Terraces succeeded");
      LoadTerracesDone();
      myTerraces.SetFeatures();
      LoadCostShares();
      SetCostShares();
      SetCustom();

      var sect = document.getElementById("uxTerraceExistsStuff");
      if (ParseInt10(myTerraces.count) > 0) { RemoveClass(sect, "display-none"); }
      else { AddClass(sect, "display-none"); }
    })
    .fail(function (jqXHR, textStatus, errorThrown) {
      var errorResult = errorThrown;
      HiUser(errorResult, "Get Terraces failed.");
    })
    .always(function () {
      SetWebServiceIndicators(false);
      ClearTerraceSelection();
    });
    //    terracesRetrievedIndx = editingFeatIndx; //store terrace indx for reselection
  } catch (err) { HiUser(err, "Load Terraces"); SetWebServiceIndicators(false); }
}
function LoadTerracesDone() {
  try {
    var info = "";
    if (!terracesJsonD || !terracesJsonD.terraces || terracesJsonD.terraces.length === 0) {
      info = "You do not have any terraces.";
    }

    RenderTerraces();
    $("[id$=uxTerracesInfo]").html(info); //set after linking or DNE
  } catch (e) { HiUser(e, "Load Terraces Done"); }
}
function RenderTerraces() {
  try {
    if (!terracesJsonD || !terracesJsonD.terraces || terracesJsonD.terraces.length === 0) {
      terracesJson = {};
      //return;
    }
    var terracesJsonTerraces = terracesJsonD.terraces;
    terracesJson = {
      terraces: terracesJsonTerraces
      , selectedID: (terracesJsonTerraces && terracesJsonTerraces.length > 0) ? terracesJsonTerraces[0].datumRecord.GUID : '0'
      , selected: function () {
        try {
          for (var i = 0; i < terracesJsonTerraces.length; i++) {
            if (terracesJsonTerraces[i].datumRecord.GUID === this.selectedID) {
              return terracesJsonTerraces[i];
            }
          }
        } catch (e) { HiUser(e, "Show Terraces selected"); }
        return {};
      }
    };
    FleshOutTerraces();
    MakeTerraceTemplate(); //do before accordions

    //terracesJson.selected.depends = "selectedID";

    //   terracesTmpl.link("#uxTerraceContainer", terracesJson);
    //editTerracesTmpl.link("#uxEditTerraceContainer", terracesJson);
    //   SetAccordions();
    SetTerraceAccordions();
    //$("#uxAccordionFeatureTools").append($("#uxTerraceContainer").innerHTML);
    //$('input:radio[name*="TerraceSelect"]').off('click').on('click', function (e) { SelectTerrace(this, e); });

  } catch (e) { HiUser(e, "Render Terraces"); }
}
function FleshOutTerraces() {
  try {
    var feats = terracesJson.terraces;
    var feat, featRec;
    for (var f in feats) {
      feat = feats[f];
      featRec = feat.terraceRecord;
      if (!featRec) continue;
      var typeRec = GetTypeByPropVal(terraceTypes, "code", featRec.FeatureID);
      //console.log("typeRec", typeRec);
      feat.propSet = terraceTypes.properties[typeRec];
      feat.geometry = new GGLMAPS.Polyline();
      SetTerraceGeometry(feat, featRec.Coords, featRec.ObjectID);
      feat.Show = function () { this.geometry.setMap(gglmap); };
      feat.Hide = function () { this.geometry.setMap(null); };
    }
  } catch (e) { HiUser(e, "Flesh Out Terraces"); }
}
function SetTerraceGeometry(feat, featCoords, featOid) {
  try {
    polyPoints = CreateMvcPointArray(featCoords);
    PrepareTerrace(terraceStyles, feat.propSet["value"]);
    feat.geometry = polyShape;
    feat.geometry.parent = featOid;
    feat.geometry.bounds = GetBoundsForPoly(polyShape);
    feat.geometry.center = GetCenterOfCoordsString(featCoords);
    AddFeatureClickEvents(feat.geometry, featureTypes.TERRACE, featureGeometrys[2], featOid);
    ClearDrawingEntities();
  } catch (e) { HiUser(e, "Set Terrace Geometry"); }
}
function MakeTerraceTemplate() {
  try {
    var html = "";
    var featsByType;
    var feat, featRec, featOid;
    var catLen = terraceCategorys.length;
    var cat;
    var checked = "";
    var numFeats;
    //Make Custom holding template, use Smooth to get count
    cat = "Smooth";
    featsByType = GetFeaturesByType(cat);
    featsByType = myTerraces.Sort("Ordinal", false, featsByType);
    numFeats = featsByType.length;
    cat = "Custom";
    html += "<h3 id='uxTerraceTypeHeader" + cat + "' class='header-split accord-header-items accord-display-none'>";
    html += "<span>" + cat + "</span>";
    html += "<label class='right'><input type='checkbox' " + checked + " data-cat=\"" + cat + "\" />" + "Show" + "</label>";
    html += "</h3>";
    html += "<div>";
    for (var featIx = 1; featIx <= numFeats; featIx++) {
      html += MakeTerraceTemplateCustom(featIx);
    }
    html += "</div>";
    //Add Conventional
    for (var catIx = 0; catIx < catLen; catIx++) {
      cat = terraceCategorys[catIx];
      if (cat === "Smooth") checked = "checked";
      else checked = "";
      html += "<h3 id='uxTerraceTypeHeader" + catIx + "' class='header-split accord-header-items accord-display-none'>";
      html += "<span>" + cat + "</span>";
      html += "<label class='right'><input type='checkbox' " + checked + " data-cat=\"" + cat + "\" />" + "Show" + "</label>";
      html += "</h3>";
      html += "<div>";
      featsByType = GetFeaturesByType(cat);
      featsByType = myTerraces.Sort("Ordinal", false, featsByType);
      numFeats = featsByType.length;
      for (var featIx = 0; featIx < numFeats; featIx++) {
        feat = featsByType[featIx];
        html += MakeTerraceTemplateFeature(feat);
      }
      html += "</div>";
    }
    //Add Key Terraces, just loop till not found and exit. Should match number of others found, but just in case.
    var keyCounter = 0;
    for (var catIx = catLen; catIx < 30; catIx++) {
      keyCounter++;
      cat = "Key Terrace " + keyCounter.toString();
      featsByType = GetFeaturesByType(cat);
      if (!featsByType || Object.keys(featsByType).length === 0) break;
      html += "<h3 id='uxTerraceTypeHeader" + catIx + "' class='header-split accord-header-items accord-display-none'>";
      html += "<span>" + cat + "</span>";
      html += "<label class='right'><input type='checkbox' data-cat=\"" + cat + "\" />" + "Show" + "</label>";
      html += "</h3>";
      html += "<div>";
      featsByType = myTerraces.Sort("Ordinal", false, featsByType);
      numFeats = featsByType.length;
      for (var featIx = 0; featIx < numFeats; featIx++) {
        feat = featsByType[featIx];
        html += MakeTerraceTemplateFeature(feat);
      }
      html += "</div>";
    }
    var holder=document.getElementById("uxTerraceMain");
    holder.innerHTML = html;
    var $holder = $(holder);
    var inputs = $holder.find('input');
    inputs.filter(':checkbox').filter("[data-cat]").on('click', function (e) { ToggleTerraceType(this, e); });
    inputs.filter(':button').filter("[data-function='highlight']").on('click', function (e) { HighlightTerrace(this, e); });
    inputs.filter(':button').filter("[data-function='customize']").on('click', function (e) { SelectToCustom(this, e); });
    inputs.filter(':button').filter("[data-function='remove']").on('click', function (e) { RemoveFromCustom(this, e); });
  } catch (e) { HiUser(e, "Make Terrace Template"); }
}
function MakeTerraceTemplateCustom(featIx) {
  var html = "";
  try {
    html += "<h3 id='uxTerraceHeaderCustom" + featIx + "' class='header-split accord-header-items accord-display-none notaccordion'>";
    html += "<label data-field='CustomText'>" + featIx + "</label>";
    html += "<span class='right'>";
    //html += "<input type='button' class='small' data-function='remove' data-id='" + 0 + "' value='Remove' />";
    html += "<input type='button' class='small' data-function='highlight' data-id='" + 0 + "' value='Highlight' />";
    html += "</span>";
    html += "</h3>";
    html += "<div>" + "<ul>" + "<li data-field='TotalCostHolder'>";
    html += "<label data-field='TotalCost'></label>";
    html += "</li>" + "</ul>" + "</div>";
  } catch (e) { HiUser(e, "Make Terrace Template Custom"); }
  return html;
}
function MakeTerraceTemplateFeature(feat) {
  var html = "";
  try {
    var featOid = feat.terraceRecord["ObjectID"];
    html += "<h3 id='uxTerraceHeader" + featOid + "' class='header-split accord-header-items accord-display-none notaccordion'>";
    html += "<label>" + feat.terraceRecord["Ordinal"].toString() + " Length " + feat.terraceRecord["Length"].toFixed(1) + " ft" + "</label>";
    html += "<input type='hidden' id='uxTerraceOid" + featOid + "' value='" + featOid + "' />";
    html += "<input type='hidden' id='uxTerraceGuid" + featOid + "' value='" + feat.datumRecord["GUID"].toString() + "' /> ";
    html += "<span class='right'>";
    html += "<input type='button' class='small' data-function='customize' data-id='" + featOid + "' value='Select to Custom' />";
    html += "<input type='button' class='small' data-function='highlight' data-id='" + featOid + "' value='Highlight' />";
    html += "</span>";
    html += "</h3>";
    html += "<div>" + "<ul>" + "<li>";
    html += "</li>" + "<li>";
    html += "<label>Cost ($/ft): </label>";
    html += "<select data-select='costPerFt' data-id='" + featOid + "' id='uxTerraceCostPerFt" + featOid + "' onchange='SetTerraceCost(this);'></select>";
    html += "</li>" + "<li data-field='TotalCostHolder'>";
    html += "<label>Total Cost ($): </label>";
    html += "<label data-field='totalCost' data-id='" + featOid + "'>0.00</label>";
    //html += "<label id='uxTerraceTotalCost" + featOid + "'>0.00</label>";
    html += "</li>" + "</ul>" + "</div>";
  } catch (e) { HiUser(e, "Make Terrace Template Feature"); }
  return html;
}
function InitToggle() {
  var type = "Smooth";
  var checked = true;
  var feats = myTerraces.features;
  var featRec, featType;
  try {
    for (var feat in feats) {
      if (feats.hasOwnProperty(feat) && feats[feat] && feats[feat].terraceRecord) {
        //feat.Show();
        featRec = feats[feat].terraceRecord;
        if (featRec) {
          featType = featRec.Type.toString().trim().toLowerCase();  
          if (featType == type.toLowerCase()) { myTerraces.Toggle(featRec.ObjectID, checked); }
        }
      }
    }
  } catch (err) { HiUser(err, "Toggle Terrace Type"); }
}
function SelectToCustom(sendr, e) {
  var oid = sendr.getAttribute("data-id");
  if (!oid) return null;
  try {
    var found = false;
    var parent = sendr;
    while (!found) {
      parent = parent.parentNode;
      if (parent.tagName.toString().toLowerCase() == "h3") found = true;
    }
    var div = parent.nextSibling;
    var feat = myTerraces.GetFeatureByOid(oid);
    var ord = feat.terraceRecord.Ordinal;
    var type = feat.terraceRecord.Type;
    var customH = document.getElementById("uxTerraceHeaderCustom" + ord);
    var $customH = $(customH);
    $customH.find("[data-function='highlight']").attr("data-id", oid);
    $customH.find("[data-function='remove']").attr("data-id", oid);
    $customH.find('input:button').filter("[data-function='highlight']").on('click', function (e) { HighlightTerrace(this, e); });
    $customH.find("[data-field='CustomText']").html("Parallel " + ord + " from " + type);
    var $div = $customH.next();
    $div.find("[data-field='TotalCostHolder']").html($(div).find("[data-field='TotalCostHolder']").html());
  } catch (err) { HiUser(err, "Select To Custom"); }

  //ORIGINAL, but don't really need other info here.
  //try {
  //  var found = false;
  //  var parent = sendr;
  //  while (!found) {
  //    parent = parent.parentNode;
  //    if (parent.tagName.toString().toLowerCase() == "h3") found = true;
  //  }
  //  var div = parent.nextSibling;
  //  var feat = myTerraces.GetFeatureByOid(oid);
  //  var ord = feat.terraceRecord.Ordinal;
  //  var customH = document.getElementById("uxTerraceHeaderCustom" + ord);
  //  customH.innerHTML = parent.innerHTML;
  //  var $customH = $(customH)
  //  $customH.find("[data-function='customize']").hide();
  //  customH.nextSibling.innerHTML = div.innerHTML;
  //  $customH.find('input:button').filter("[data-function='highlight']").on('click', function (e) { HighlightTerrace(this, e); });
  //} catch (err) { HiUser(err, "Select To Custom"); }
  e.stopPropagation(); //allows control to work in accordion header
}
//TODO: rewrite if this is needed
function RemoveFromCustom(sendr, e) {
  //var oid = sendr.getAttribute("data-id");
  //if (!oid) return null;
  //try {
  //  var found = false;
  //  var parent = sendr;
  //  while (!found) {
  //    parent = parent.parentNode;
  //    if (parent.tagName.toString().toLowerCase() == "h3") found = true;
  //  }
  //  var feat = myTerraces.GetFeatureByOid(oid);
  //  var ord = feat.terraceRecord.Ordinal;
  //  var type = feat.terraceRecord.Type;
  //  var customH = document.getElementById("uxTerraceHeaderCustom" + ord);
  //  var $customH = $(customH);
  //  $customH.find("[data-function='highlight']").attr("data-id", oid);
  //  $customH.find('input:button').filter("[data-function='highlight']").on('click', function (e) { HighlightTerrace(this, e); });
  //  $customH.find("[data-field='CustomText']").html("Parallel " + ord + " from " + type);
  //} catch (err) { HiUser(err, "Select To Custom"); }
  e.stopPropagation(); //allows control to work in accordion header
}
function HighlightTerrace(sendr, e) {
  var oid = sendr.getAttribute("data-id");
  if (!oid || ParseInt10(oid) === 0) return null;
  try {
    myTerraces.HighlightFeature(oid);
  } catch (err) { HiUser(err, "Highlight Terrace"); }
  e.stopPropagation(); //allows control to work in accordion header
}
function ToggleTerraceType(sendr, e) {
  var type = sendr.getAttribute("data-cat");
  if (!type) return null;
  if (type === "Custom") { ToggleTerraceTypeCustom(sendr); e.stopPropagation(); return; }
  var checked = sendr.checked;
  var feats = myTerraces.features;
  var featRec, featType;
  try {
    for (var feat in feats) {
      featRec = feats[feat].terraceRecord;
      if (featRec) {
        featType = featRec.Type.toString().trim().toLowerCase();
        if (featType == type.toLowerCase()) { myTerraces.Toggle(featRec.ObjectID, checked); }
      }
    }
  } catch (err) { HiUser(err, "Toggle Terrace Type"); }
  e.stopPropagation(); //allows control to work in accordion header
}
function ToggleTerraceTypeCustom(sendr) {
  var checked = sendr.checked; //to show or not
  var isChecked = false; //is type checked or not
  //if unchecking Custom but Type is checked, then don't hide.
  try {

    var desiredParent = null;
    var parent = sendr;
    while (!desiredParent) {
      parent = parent.parentNode;
      if (parent.tagName.toString().toLowerCase() == "h3") desiredParent = true;
    }
    console.log(parent);
    var div = $(parent).next();
    var idCtls = div.find("[data-id]");
    var headers = $(document.getElementById("uxTerraceContainer")).find("[id^='uxTerraceTypeHeader']");
    var oid, feat, type, header, isChecked;
    idCtls.each(function () { 
      $this = $(this);
      oid = ParseInt10($this.attr("data-id"));
      console.log("oid", oid);
      //see if original is checked
      feat = myTerraces.GetFeatureByOid(oid);
      if (!feat) return true;//next
      type = feat.terraceRecord.Type;
      header = headers.find("[data-cat='" + type + "']");
      isChecked = header.prop("checked");
      console.log(type, isChecked);
      if (!checked && isChecked) return true;//next
      myTerraces.Toggle(oid, checked);
    });


  //var feats = myTerraces.features;
  //var featRec, featType;
    //for (var feat in feats) {
    //  featRec = feats[feat].terraceRecord;
    //  if (featRec) {
    //    featType = featRec.Type.toString().trim().toLowerCase();
    //    if (featType == type.toLowerCase()) { myTerraces.Toggle(featRec.ObjectID, checked); }
    //  }
    //}
  } catch (err) { HiUser(err, "Toggle Terrace Type Custom"); }
}
function GetFeaturesByType(type) {
  if (!type) return null;
  var retVal = {};
  var countr = 0;
  var feats = terracesJsonD.terraces;
  var featRec, featType;
  try {
    for (var feat in feats) {
      featRec = feats[feat].terraceRecord;
      if (featRec) {
        featType = featRec.Type.toString().trim().toLowerCase();
        if (featType == type.toLowerCase()) { retVal[countr] = feats[feat]; countr++ }
      }
    }
  } catch (err) { HiUser(err, "Get Terraces By Type"); }
  return retVal;
}

function HideAllTerraces() {
  if (myTerraces.count > 0) {
    myTerraces.Hide();
    $(document.getElementById("uxTerraceContainer")).find("input:checkbox").filter("[data-cat]").attr('checked', false);
  }
}
function CalculateTerraces(sendr) {
  try {
    var isTerraces = (ParseInt10(myTerraces.count) > 0) ? true : false;
    if (isTerraces) {
      var confMsg = CR + CR;
      confMsg += "Press okay to remove all current terraces and recalculate." + CR + CR + CR;
      var YorN = confirm(confMsg);
      if (!YorN) return false;
    }
    //document.getElementById("uxTerraceContainer").innerHTML = "Wait for processing. Then Load Terraces.";
    var projId = GetProjId();
    var svcData = {};
    svcData["projectId"] = ParseInt10(projId);

    var origVal = sendr.value;
    sendr.value = "Processing...";
    sendr.setAttribute("disabled", true);
    myTerraces.Reset();
    //  Public Function CalculateTerraces(ByVal projectId As Long) As String
    $.ajax({
      url: "GISTools.asmx/CalculateTerraces"
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
      HiUser(msg, "Calculate Terraces failed or timed out.");
    })
    .always(function () {
      SetWebServiceIndicators(false);
      sendr.value = origVal;
      sendr.removeAttribute("disabled");
    });
  } catch (e) { HiUser(e, "Submit Calculate Terraces"); SetWebServiceIndicators(false); }
}
function LoadTerraces() {
  try {
    var projId = GetProjId();

    var svcData = {};
    svcData["projectId"] = ParseInt10(projId, 10);

    SetWebServiceIndicators(true, "Loading Terraces");
    myTerraces.Reset();
    //  Public Function LoadTerraces(ByVal projectId As Long) As TerracePackageList
    $.ajax({
      url: "GISTools.asmx/LoadTerraces"
      , data: JSON.stringify(svcData)
    })
    .done(function (data, textStatus, jqXHR) {
      terracesJsonD = data.d;
      if (terracesJsonD.info && terracesJsonD.info.trim().length > 0) HiUser(terracesJsonD.info, "Load Terraces succeeded");
      if (!terracesJsonD.terraces) { myTerraces.Reset(); HiUser("No terraces are available", "Load Terraces succeeded"); }
      LoadTerracesDone();
      myTerraces.SetFeatures();
    })
    .fail(function (jqXHR, textStatus, errorThrown) {
      var msg = "";
      if (textStatus) msg += "Status: " + textStatus;
      if (errorThrown) msg += "\nError: " + errorThrown;
      HiUser(msg, "Load Terraces failed.");
    })
    .always(function () {
      SetWebServiceIndicators(false);
    });
  } catch (e) { HiUser(e, "Submit Load Terraces"); SetWebServiceIndicators(false); }
}

function SetTerraceAccordions() {
  try {
    var cont = document.getElementById("uxTerraceContainer");

    var foundExpanded = false;
    var $notaccordion = $(cont).find(".notaccordion");
    $notaccordion.addClass("ui-accordion ui-accordion-icons ui-widget ui-helper-reset")
    .find("h3")
    .addClass("ui-accordion-header ui-helper-reset ui-state-default ui-corner-top ui-corner-bottom")
    .each(function () { // Iterate over items
      var $expander = $(document.createElement('span'));
      $expander.addClass('ui-icon ui-icon-triangle-1-e');
      if (0 == $(this).find('span.ui-icon').length) $(this).prepend($expander);
      if (0 < $(this).find('span.ui-icon.ui-icon-triangle-1-s').length) foundExpanded = true;
    })
    .unbind('hover')
    .hover(function () { $(this).toggleClass("ui-state-hover"); })
    .unbind('click')
    .click(function () {
      $(this)
        .toggleClass("ui-accordion-header-active ui-state-active ui-state-default ui-corner-bottom")
        .find("> .ui-icon").toggleClass("ui-icon-triangle-1-e ui-icon-triangle-1-s").end()
        .next().toggleClass("ui-accordion-content-active").slideToggle();
      return false;
    })
    .next()
    .addClass("ui-accordion-content ui-helper-reset ui-widget-content ui-corner-bottom")
    .hide();

    //if (!foundExpanded) $notaccordion.find("h3").first().click();
    $notaccordion.find("h3").removeClass("accord-display-none");
  } catch (e) { HiUser(e, "Set Terrace Accordions"); }
}
function ToggleTerrace(sendr) {
  var div = null;
  var parent = sendr;
  while (!div) {
    parent = parent.parentNode;
    if (parent.tagName.toString().toLowerCase() == "div") div = true;
  }
  var base = parent.id;
  var oidCtl = base.replace("Details", "Oid");
  var oid = document.getElementById(oidCtl).value;
  myTerraces.Toggle(oid, sendr.checked);
}
function ToggleTerraceHighlight(sendr) {
  var div = null;
  var parent = sendr;
  while (!div) {
    parent = parent.parentNode;
    if (parent.tagName.toString().toLowerCase() == "div") div = true;
  }
  var base = parent.id;
  var oidCtlName = base.replace("Details", "Oid");
  var oidCtl = document.getElementById(oidCtlName);
  var oid = oidCtl.value;
  myTerraces.ToggleHighlight(oid, sendr.checked);
  var togCtl = document.getElementById(sendr.id.replace("Highlight", ""));
  if (sendr.checked) togCtl.checked = true;
  if (!sendr.checked && !togCtl.checked) { myTerraces.Toggle(oid, false); }
}
function SetToggleTerrace(tOrF) {
  var inputs = document.getElementById("uxTerraceContainer").getElementsByTagName("input");
  var input;
  for (var inIx = 0; inIx < inputs.length; inIx++) {
    input = inputs[inIx];
    if (input.id.indexOf("ToggleTerrace") > -1) {
      if (tOrF) {
        if (input.id.indexOf("Highlight") < 0) { input.checked = tOrF; }
      } else {
        input.checked = tOrF;
      }
    }
  }
}
function LoadCostShares() {
  try {
    var sels = $("[data-select='costPerFt']");
    var csLen = costShares.length;
    var cs;
    sels.each(function () { // Iterate over items
      $this = $(this);
      $this.find('option').remove();
      for (var csIx = 0; csIx < csLen; csIx++) {
        cs = costShares[csIx];
        $this.append('<option value=' + csIx + '>(' + parseFloat(cs["CostPerFt"]).toFixed(2) + ") " + cs["Description"] + '</option>');
      }
    });

  } catch (e) { HiUser(e, "Load Cost Shares"); }
}
function SetCostShares() {
  try {
    var sels = $("[data-select='costPerFt']");
    var csLen = costShares.length;
    var cs;
    sels.each(function () { // Iterate over items
      $this = $(this);
      var oid = $this.attr("data-id");
      if (oid) {
        var feat = myTerraces.GetFeatureByOid(oid);
        if (feat) {
          $this.val(feat.terraceRecord.CostShareID);
          $this.change();
        } else {
          console.log("no feat for ", oid);
        }
      }
    });

  } catch (e) { HiUser(e, "Set Cost Shares"); }
}
function SetCustom() {
  try {
    var cont = $(document.getElementById("uxTerraceContainer"));
    var headers = cont.find("h3").filter("[id^='uxTerraceHeader']").not("[id^='uxTerraceHeaderCustom']"); 
    
    var oid, feat, div;
    headers.each(function () { 
      $this = $(this);
      oid = ParseInt10(this.id.replace("uxTerraceHeader", ""));
      feat = myTerraces.GetFeatureByOid(oid); 
      if (feat.terraceRecord.Custom != true) return true;//next
      div = $this.find("input:button").filter("[data-function='customize']").click();
    });

  } catch (e) { HiUser(e, "Set Custom"); }
}

function SetTerraceCosts(sendr, id) {
  try {
    var mainId = id + "Main";
    var notCtl = $("#" + mainId);
    var setVal = notCtl.val();
    $(document.getElementById("uxTerraceContainer")).find("[id^=" + id + "]").not(notCtl).each(function () {
      try {
        $this = $(this);
        thisid = $this.attr("id");
        if (thisid == mainId) console.log("SetTerraceCosts 'not' didn't work.");
        //console.log(thisid);
        $this.val(setVal);
        $this.change();

      } catch (e) { HiUser(e, "Set cost value for " + thisid); }
    });

  } catch (e) { HiUser(e, "Set Terrace Costs"); }

  //////ORIGINAL
  //try {
  //  var mainId = id + "Main";
  //  var notCtl = $("#" + mainId);
  //  var setVal = notCtl.val();
  //  $("#uxTerraceContainer").find("[id^=" + id + "]").not(notCtl).each(function () { // Iterate over inputs
  //    try {
  //      $this = $(this);
  //      thisid = $this.attr("id");
  //      if (thisid == mainId) console.log("not didn't work.");
  //      console.log(thisid);
  //      $this.val(setVal);
  //      $this.change();

  //    } catch (e) { HiUser(e, "Set cost value for " + thisid); }
  //  });

  //} catch (e) { HiUser(e, "Set Terrace Costs"); }
}
function SetTerraceCost(sendr) {
  try {

    var perFt = costShares[sendr.value]["CostPerFt"];

    var oid = sendr.getAttribute("data-id");
    var $dest = $(document.getElementById("uxTerraceContainer")).find("[data-field='totalCost']").filter("[data-id='" + oid + "']");
    var feat = myTerraces.GetFeatureByOid(oid);
    var featLen = feat.terraceRecord.Length;
    var total = featLen * perFt;
    $dest.text(parseFloat(total).toFixed(2));
  } catch (e) { HiUser(e, "Set Terrace Cost"); }

  //////ORIGINAL
  //try {
  //  var dest = document.getElementById(sendr.id.replace("CostPerFt", "TotalCost"));
  //  var perFt = costShares[sendr.value]["CostPerFt"];

  //  var div = null;
  //  var parent = sendr;
  //  while (!div) {
  //    parent = parent.parentNode;
  //    if (parent.tagName.toString().toLowerCase() == "div") div = true;
  //  }
  //  var base = parent.id;
  //  var oidCtl = base.replace("Details", "Oid");
  //  var oid = document.getElementById(oidCtl).value;
  //  var feat = myTerraces.GetFeatureByOid(oid);
  //  var featLen = feat.terraceRecord.Length;
  //  var total = featLen * perFt;
  //  dest.innerText = parseFloat(total).toFixed(2);
  //} catch (e) { HiUser(e, "Set Terrace Cost"); }
}
function ShowTerraceReport() {
  try {
    var projId = GetProjId();

    var svcData = {};
    svcData["projectId"] = ParseInt10(projId, 10);

    SetWebServiceIndicators(true, "Loading Report");
    $.ajax({
      url: "GISTools.asmx/ShowTerraceReport"
      , data: JSON.stringify(svcData)
    })
    .done(function (data, textStatus, jqXHR) {
    })
    .fail(function (jqXHR, textStatus, errorThrown) {
      var msg = "";
      if (textStatus) msg += "Status: " + textStatus;
      if (errorThrown) msg += "\nError: " + errorThrown;
      HiUser(msg, "Show Terrace Report failed.");
    })
    .always(function () {
      SetWebServiceIndicators(false);
    });
  } catch (e) { HiUser(e, "Show Terrace Report"); }
}
function OpenReportPage() {
  try {
    var projectId = ParseInt10(GetControlByTypeAndId("input", "uxHiddenProjectId").value);
    window.open('http://' + GetHostName() + '/Members/TerraceRunReport.aspx?project=' + projectId, 'terracereport'); //opens new window this way
  } catch (e) { HiUser(e, "Open Report Page"); }
  return false;
}
function SubmitTerraceTool(sendr) {
  try {
    //SaveTerraceCosts(sendr);
    SaveCustom();
  } catch (e) { HiUser(e, "Submit Terrace Tool"); }
}
function SaveTerraceCosts(sendr) {
  try {
    var cont = document.getElementById("uxTerraceContainer");
    var base = "uxTerraceHeader";
    var headers = $(cont).find("[id^=" + base + "]");
    console.log("headers", headers.length);
    var oidCtl, oid;
    var costCtl, cost;
    headers.each(function () {
      try {
        $this = $(this);
        thisid = $this.attr("id");
        oidCtl = document.getElementById(thisid.replace("Header", "Oid"));
        oid = oidCtl.value;
        costCtl = document.getElementById(thisid.replace("Header", "CostPerFt"));
        cost = costCtl.value;

        SubmitTerraceCost(oid, cost);
      } catch (e) { HiUser(e, "Save Terrace Costs Iterate Headers " & thisid); }
    });

  } catch (e) { HiUser(e, "Save Terrace Costs"); }
}
function SubmitTerraceCost(oid, cost) {
  try {
    var projId = GetProjId();
    var svcData = {};
    svcData["projectId"] = ParseInt10(projId);
    svcData["featureId"] = ParseInt10(oid);
    svcData["costId"] = ParseInt10(cost);

    $.ajax({
      url: "GISTools.asmx/SubmitTerraceCost"
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
      HiUser(msg, "Submit Terrace Cost failed or timed out.");
    })
    .always(function () {
      SetWebServiceIndicators(false);
    });
  } catch (e) { HiUser(e, "Submit Terrace Cost"); SetWebServiceIndicators(false); }
}
function SaveCustom() {
  try {
    //send in Custom Ids. Other Ids will be removed from Custom status in db.
    var featureIds = [];
    var header = $(document.getElementById("uxTerraceTypeHeaderCustom"));
    var div = header.next();
    var featHeaders = div.find("h3");
    var featDivs = featHeaders.next();
    var dataIds = featDivs.find("[data-id]");
    var oid, feat;
    dataIds.each(function () {
      try {
        $this = $(this);
        oid = $this.attr("data-id");
        feat = myTerraces.GetFeatureByOid(oid);
        if (feat && featureIds.indexOf(oid) < 0) featureIds.push(ParseInt10(oid));
      } catch (e) { HiUser(e, "Save Custom Iterate DataIds " & thisid); }
    });
    console.log(featureIds);
    //return;
    var projId = GetProjId();
    var svcData = {};
    svcData["projectId"] = ParseInt10(projId);
    svcData["featureIds"] = featureIds;

    $.ajax({
      url: "GISTools.asmx/SaveCustom"
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
      HiUser(msg, "Save Custom failed or timed out.");
    })
    .always(function () {
      SetWebServiceIndicators(false);
    });
  } catch (e) { HiUser(e, "Save Custom"); SetWebServiceIndicators(false); }
}

function OpenTerraceList() { OpenForm("uxTerrace"); }