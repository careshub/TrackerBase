<%@ Page Title="" Language="VB" MasterPageFile="~/Site.master" AutoEventWireup="false" CodeFile="ProjectHome.aspx.vb"
  Inherits="ProjectHome" EnableEventValidation="false" %>

<%@ MasterType VirtualPath="~/Site.master" %>

<asp:Content ID="Content1" ContentPlaceHolderID="HeadContent" runat="Server">
  <link href="/Styles/toggle-switch.css" rel="stylesheet" type="text/css" />
  <link href="/Styles/ProjectHome.css?v=20150918" rel="stylesheet" type="text/css" />
  <script type="text/javascript" id="uxPageJS">var costShares = [];</script>
  <asp:Literal ID="paramHolder" runat="server" />
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="Server">
  <asp:HiddenField ID="uxHiddenProjectAddress" runat="server" />
  <asp:HiddenField ID="uxHiddenProjectCity" runat="server" />
  <asp:HiddenField ID="uxHiddenProjectRegion" runat="server" />
  <asp:HiddenField ID="uxHiddenProjectRegionAbbr" runat="server" />
  <asp:HiddenField ID="uxHiddenProjectZip" runat="server" />
  <asp:HiddenField ID="uxHiddenProjectSubRegion" runat="server" />
  <asp:HiddenField ID="uxHiddenProjectSubRegionCode" runat="server" />
  <asp:HiddenField ID="uxHiddenProjectRegionLatLons" runat="server" />
  <asp:HiddenField ID="uxHiddenProjectSubRegionLatLons" runat="server" />
  <asp:HiddenField ID="uxHiddenProjectLocation" runat="server" />

  <div id="uxContainer">
    <%-- main div to contain inline page elements --%>

    <div id="uxProjectInfoContainer" class="" title="This shows the project name and location">
      <span class="info-text" id="uxProjectLocationHeader">Location of <span class="info-text" id="uxProjectName"></span>:</span>
      <span class="info-text" id="uxProjectLocation"></span>
      <div class="right map-view-control">
        <label class="checkbox toggle candy" onclick="" title="Set to Off to see a table view of features instead of the map view.">
          <input id="uxToggleMapView" type="checkbox" onclick="ToggleMapView(this.checked, true);" checked />
          <p>
            <span>On</span>
            <span>Off</span>
          </p>
          <a class="slide-button"></a>
        </label>
      </div>
      <span class="info-text right" id="uxProjectView">Map View:</span>
    </div>
    <label runat="server" id="uxInfo"></label>

    <div class="col--mask right--menu clear">
      <%-- <div class="col--left">--%>
      <div class="col--2 home-menu">
        <div id="uxMapControls" class="">
          <div id="uxAccordionNav" class="accordion collapsible">
            <h3>Map Controls</h3>
            <div style="overflow: hidden;">
              <ul id="uxNavTools">
                <li>
                  <div class=" left  " style="width: 40%;" title="Click On to use your scroll wheel to zoom in and out on the map">
                    <label class="checkbox toggle candy" onclick="">
                      <input id="uxToggleZoomWheel" type="checkbox" checked="checked" onclick="toggleZoom(!this.checked)" />
                      <p><span>On</span><span>Off</span></p>
                      <a class="slide-button"></a>
                    </label>
                  </div>
                  <div class="valign-outer right" style="width: 55%;">
                    <div class="valign-middle">
                      <div class="valign-inner">Mouse wheel zoom</div>
                    </div>
                  </div>
                  <br />
                  <br />
                </li>
                <li>
                  <div class=" left" style="width: 40%;" title="Click to toggle visibility of administrative boundaries">
                    <label class="checkbox toggle candy" onclick="">
                      <input id="uxToggleAdminBdry" type="checkbox" onclick="showAdminBdryOverlay = !showAdminBdryOverlay; ToggleAdminBdry();" />
                      <p><span>On</span><span>Off</span></p>
                      <a class="slide-button"></a>
                    </label>
                  </div>
                  <div class="valign-outer right" style="width: 55%;">
                    <div class="valign-middle">
                      <div class="valign-inner">
                        Administrative Boundaries
                        <img src="/images/about.gif" alt="Administrative boundaries layer information."
                          title="Shows state and county boundaries."
                          onmouseover="$('[id$=uxLayerInfo]').removeClass('display-none');$('[id$=uxLayerInfo]').html('Toggles state and county boundary visibility.<br />');"
                          onmouseout="$('[id$=uxLayerInfo]').addClass('display-none');$('[id$=uxLayerInfo]').html('');" />
                      </div>
                    </div>
                  </div>
                  <br />
                  <br />
                </li>
                <li>
                  <div class=" left" style="width: 40%;" title="Click to toggle visibility of the soils layer">
                    <label class="checkbox toggle candy" onclick="">
                      <input id="uxToggleSoils" type="checkbox" onclick="showSoilsOverlay = !showSoilsOverlay; ToggleSoils();" />
                      <p><span>On</span><span>Off</span></p>
                      <a class="slide-button"></a>
                    </label>
                  </div>
                  <div class="valign-outer right" style="width: 55%;">
                    <div class="valign-middle">
                      <div class="valign-inner">
                        Soils
                        <img src="/images/about.gif" alt="Soils layer information."
                          title="Soils layer is only visible at closer zoom levels."
                          onmouseover="$('[id$=uxLayerInfo]').removeClass('display-none');$('[id$=uxLayerInfo]').html('Soils layer is only visible at closer zoom levels.<br />');"
                          onmouseout="$('[id$=uxLayerInfo]').addClass('display-none');$('[id$=uxLayerInfo]').html('');" />
                      </div>
                    </div>
                  </div>
                  <br />
                  <br />
                </li>
                <li>
                  <div class=" left" style="width: 40%;" title="Click to toggle topographic background">
                    <label class="checkbox toggle candy" onclick="">
                      <input type="checkbox" onclick="showTopo = !showTopo; ToggleTopo();" />
                      <p><span>On</span><span>Off</span></p>
                      <a class="slide-button"></a>
                    </label>
                  </div>
                  <div class="valign-outer right" style="width: 55%;">
                    <div class="valign-middle">
                      <div class="valign-inner">
                        USA Topo<img src="/images/about.gif" alt="Topo map layer information."
                          title="USA Topo map is not visible beyond a certain zoomed-in level."
                          onmouseover="$('[id$=uxLayerInfo]').removeClass('display-none');$('[id$=uxLayerInfo]').html('USA Topo map is not visible beyond a certain zoomed-in level.<br />');"
                          onmouseout="$('[id$=uxLayerInfo]').addClass('display-none');$('[id$=uxLayerInfo]').html('');" />
                      </div>
                    </div>
                  </div>
                  <br />
                  <br />
                </li>
                <li>
                  <div id="uxLayerInfo" class="display-none">
                    Hover over info button to see display.<br />
                  </div>
                </li>
                <li class=" ">
                  <input type="button" class="main-menu" id="uxZoomToAllFeatures" onclick="SetMapExtentByOids();"
                    title="Set map extent to include all features for this project" value="Zoom to all Features" />
                </li>
                <li class="display-none">
                  <input type="button" class="main-menu" id="uxOpenGeometryTools" onclick="ShowGeometryTools();"
                    title="Open tools for calculating area and distance" value="Geometry Tools" />
                </li>
              </ul>
            </div>
          </div>
          <div id="uxAccordionFeatureTools" class="accordion accord-tools">
            <h3 class="step-one">Contour Tools</h3>
            <div>
              <input type="button" id="uxOpenContourTools" class="main-menu" value="Import" onclick="StartImportContourRaws(this);"
                title="Open contour import tools" />
              <input type="button" id="uxDeleteContours" class="main-menu" value="Delete" onclick="DeleteContours();"
                title="Delete existing contours" />
            </div>
            <h3 class="step-one">Terrace Area Tools</h3>
            <div>
              <input type="button" id="uxOpenFieldTools" class="main-menu" onclick="BeginNewField(this);"
                title="Open terrace area creation tools" value="Create New Area" />
              <p>
                Select area to use the following:
              </p>
              <input type="button" id="uxEditField" class="main-menu" data-sel-req="field" onclick="EditField(this);"
                title="Open terrace area editing tools" value="Edit Area" />
              <input type="button" id="uxDeleteField" class="main-menu" data-sel-req="field" onclick="DeleteField(editingOid);"
                title="Delete the selected terrace area" value="Delete Area" />
            </div>
            <h3 class="step-one">Calculate</h3>
            <div>
              <p>
                Set visibility on contour layers.
              </p>
              <p>
                <label>
                  <input type="checkbox" value="Raw" onclick="ToggleContourRaws(this);" checked />
                  Show Raw Contours</label>
              </p>
              <p>
                <label>
                  <input type="checkbox" value="Smooth" onclick="ToggleContours(this);" checked />
                  Show Smooth Contours</label>
              </p>
              <p>
                Calculate smoothed contours. This may take a half hour or so.
              </p>
              <input type="button" id="uxCalcSmooth" class="main-menu" value="Calculate"
                onclick="CalculateContours(this);" runat="server" title="Calculate smooth contours from uploaded raw contours" />
              <p>
                Load smoothed contours. Click after fortran has finished smoothing.
              </p>
              <input type="button" id="uxLoadSmooth" class="main-menu" value="Load Smooth"
                onclick="LoadSmoothContours();" runat="server" title="Load smooth contours from fortran output" />
            </div>

            <h3 class="step-two accord-display-none">High Point Tools</h3>
            <div>
              <input type="button" id="uxCreateHighPoint" class="main-menu" onclick="BeginNewHighPoint(this, 'highPoint');"
                title="Create new high point" value="Create High Point" />
              <input type="button" id="uxEditHighPoint" class="main-menu" onclick="EditHighPoint(this, 'highPoint');"
                title="Move high point to new location" value="Edit High Point" />
              <input type="button" id="uxDeleteHighPoint" class="main-menu" onclick="DeleteHighPoint(this, 'highPoint');"
                title="Delete existing high point" value="Delete High Point" />
            </div>
            <h3 class="step-two accord-display-none">Ridgeline Tools</h3>
            <div>
              <input type="button" id="uxCreateRidgelines" class="main-menu" onclick="BeginNewRidgeline(this, 'ridgeline');"
                title="Create new ridgeline" value="Create New Ridgeline" />
              <p class="display-none">
                Select a ridgeline to use the following:
              </p>
              <input type="button" id="uxEditRidgeline" class="main-menu" onclick="EditRidgeline(this, 'ridgeline');"
                title="Edit existing ridgeline" value="Edit Ridgeline" />
              <input type="button" id="uxDeleteRidgeline" class="main-menu" onclick="DeleteRidgeline(this, 'ridgeline');"
                title="Delete existing ridgeline" value="Delete Ridgeline" />
            </div>
            <h3 class="step-two accord-display-none">Divide Tools</h3>
            <div>
              <input type="button" id="uxCreateDivides" class="main-menu" onclick="BeginNewDivide(this, 'divide');"
                title="Create new divide" value="Create New Divide" />
              <input type="button" id="uxOrderDivides" class="main-menu" onclick="OpenOrderDivideTool(this);"
                title="Open tool to verify divide ordering" value="Order Divides" />
              <input type="button" id="uxAlignDivides" class="main-menu" onclick="OpenAlignDivideTool(this);"
                title="Open tool to verify direction of divides" value="Align Divides" />
              <p>
                Select a divide to use the following:
              </p>
              <input type="button" id="uxEditDivide" class="main-menu" onclick="EditDivide(this, 'divide');"
                title="Edit the selected divide" value="Edit Divide" />
              <input type="button" id="uxDeleteDivide" class="main-menu" onclick="DeleteDivide(this, 'divide');"
                title="Delete the selected divide" value="Delete Divide" />
            </div>
            <h3 class="step-two accord-display-none">Waterway Tools</h3>
            <div>
              <input type="button" id="uxCreateWaterways" class="main-menu" onclick="BeginNewWaterway(this, 'waterway');"
                title="Create new waterway" value="Create New Waterway" />
              <input type="button" id="uxOrderWaterways" class="main-menu" onclick="OpenOrderWaterwayTool(this);"
                title="Open tool to verify waterway ordering" value="Order Waterways" />
              <input type="button" id="uxAlignWaterways" class="main-menu" onclick="OpenAlignWaterwayTool(this);"
                title="Open tool to verify direction of waterways" value="Align Waterways" />
              <p>
                Select a waterway to use the following:
              </p>
              <input type="button" id="uxCopyWaterway" class="main-menu" onclick="CopyWaterway(this, 'waterway');"
                title="Copy the selected waterway into a new waterway for editing" value="Copy Waterway" />
              <input type="button" id="uxEditWaterway" class="main-menu" onclick="EditWaterway(this, 'waterway');"
                title="Edit the selected waterway" value="Edit Waterway" />
              <input type="button" id="uxDeleteWaterway" class="main-menu" onclick="DeleteWaterway(this, 'waterway');"
                title="Delete the selected waterway" value="Delete Waterway" />
            </div>
            <h3 class="step-three accord-display-none">Equipment Tools</h3>
            <div>
              <input type="button" id="uxOpenEquipmentTools" class="main-menu" onclick="OpenEquipmentTool(this);"
                title="Open equipment tools" value="Equipment Defaults" />
            </div>
            <h3 class="step-three accord-display-none">Calculate</h3>
            <div>
              <p>
                Calculate terrace options.
              </p>
              <input type="button" id="uxCalculateTerraces" class="main-menu" value="Calculate"
                onclick="CalculateTerraces(this);" runat="server" title="Calculate terraces and move to step four, terrace selection" />
              <p>
                Load terraces. Click after fortran has finished calculating.
              </p>
              <input type="button" id="uxLoadTerraces" class="main-menu" value="Load Terraces"
                onclick="LoadTerraces(); LoadSmoothContours();" runat="server" title="Load terraces from fortran output." />
            </div>
            <h3 class="step-four accord-display-none">Terraces</h3>
            <div>
              <ul>
                <li>
                  <input type="button" id="uxOpenReportPage" class="main-menu" value="Open Report Page"
                    onclick="OpenReportPage();" runat="server" title="Opens a new tab with report and error info" />
                </li>
                <li>
                  <label id="uxTerracesInfo"></label>
                </li>
                <li>
                  <input type="button" id="uxOpenTerraceList" class="main-menu" value="Open List"
                    onclick="OpenTerraceList();" runat="server" title="Opens a popup with a list of terraces." />
                </li>
                <li>
                  <input type="button" id="uxHideAll" class="main-menu" value="Hide All Terraces"
                    onclick="HideAllTerraces();" runat="server" title="Hides all terraces on the map." />
                </li>
                <li>
                  <label>Cost ($/ft): </label>
                  <select id="uxTerraceCostPerFtMain" data-select="costPerFt"></select>
                  <input type="button" id="uxSetCostPerFt" data-set="costPerFt" class="main-menu"
                    onclick="SetTerraceCosts(this, 'uxTerraceCostPerFt');" value="Set All Costs" title="Set all cost per foot selections" />
                </li>
                <%--<li>
                  <input type="button" id="uxSaveCostPerFt" data-set="costPerFt" class="main-menu"
                    onclick="SaveTerraceCosts(this);" value="Save All" title="Save all cost info to database" />
                </li>--%>
              </ul>
            </div>
            <%--<div id="uxTerraceContainer2" class="step-four display-none"></div>--%>
            <%--<h3 id="uxReportHeader" class="step-four accord-display-none">Report</h3>
            <div>
              <input type="button" id="uxCreateReport" class="main-menu" value="Report"
                onclick="Report();" runat="server" title="Create terrace report" />
            </div>--%>
          </div>
        </div>
      </div>

      <div id="uxPopupCenter" class="col--1 home-main">
        <div id="uxMapContainer" data-view="map"></div>

        <div id="uxFieldContainer" data-view="field gis"></div>
        <%-- render with templating --%>

        <script id="fieldsTmpl" type="text/x-jsrender">
          <div id="uxFieldsContainer" data-view="field gis" class="">
            <div class="clear-fix">
              <span class="text-center list-title">Terrace Area</span>
              <span class="right">
                <input type="button" value="Refresh" class="accord-button" title="Reload terrace area from server" onclick="fieldsRetrievedIndx = -99; ReloadFields(this); return false;" />
                <input type="button" value="Close" class="accord-button" title="Return to map view" onclick="$('#uxToggleMapView').trigger('click'); return false;" />
              </span>
            </div>
            <div><span id="uxFieldsInfo">You do not have a terrace area created. Use the tools button to create a new terrace area.</span></div>
            {^{for fields}}
            <div id="uxFieldAccordion{{:#index}}" class="accord-group notaccordion collapsible collapsed">
              <h3 id="uxFieldsHeader{{:#index}}" class="accord-header-items">
                <input type="hidden" id="uxFieldOid{{:#index}}" value="{{:fieldRecord.ObjectID}}" />
                <input type="hidden" id="uxFieldGuid{{:#index}}" value="{{:fieldDatum.GUID}}" />
                <input type="radio" name="FieldSelect" id="uxFieldSelect{{:#index}}" class="accord-sel" />
                <span class="accord-header-separate display-none OLD"><span>Area ID: </span><span>{{:fieldRecord.FieldName}}</span>
                  {{if !fieldRecord.Shape || fieldRecord.Shape.trim().length<1}}<span class="warning">No Shape</span>{{/if}}</span>
                <span class="accord-header-separate-6">
                  <span>Terrace Area: </span><span>{{:fieldRecord.FieldName}}</span>
                  <span>Acres: </span><span class="text-right">{{positive:fieldRecord.TotalArea}}</span>
                  {{if !fieldRecord.Shape || fieldRecord.Shape.trim().length<1}}<span class="warning">No Shape</span>{{/if}}</span>
              </h3>
              <div>
                <table id="uxSelectedFieldDetails{{:#index}}" class="field-table full-width">
                  <tbody>
                    <tr>
                      <td>Acres: </td>
                      <td>
                        <label id="uxFieldTotalArea{{:#index}}">{{positive:fieldRecord.TotalArea}}{{if !fieldRecord.Shape || fieldRecord.Shape.trim().length<1}}<span class="warning">No Shape</span>{{/if}}</label></td>
                      <td></td>
                    </tr>
                    <tr>
                    </tr>
                    <tr class="clear">
                      <td class="no-align">Created:
                        <label id="uxFieldsCreated{{:#index}}">{{SDate fieldDatum.Created /}}</label></td>
                      <td class="no-align">Edited:
                        <label id="uxFieldsEdited{{:#index}}">{{SDate fieldDatum.Edited /}}</label></td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
            {{/for}}
          </div>
        </script>

        <div id="uxEditFieldContainer" class="display-none popup-tools draggable field-tools">
          <script id="editFieldsTmpl" type="text/x-jsrender">
            {^{if selectedID && selectedID !== '0'}}
  <div id="uxEditFieldForm" class="popup--form">
    <div id="uxEditFieldHeader" class="popup-tools-header">
      <h3 id="uxEditFieldTitle" class="popup--tools-title">Edit Terrace Area</h3>
      <div class="popup-control-panel">
        <img title="This form is draggable when you see the arrows" alt="Draggable form" src="/images/move.png"
          class="control-drag" />
        <img title="Close this form" alt="Close form" src="/images/close.png"
          id="uxEditFieldDrawCancel" data-form-cancel="field-tools" class="control-close" onclick="CancelFieldDraw();" />
      </div>
    </div>
    <hr />
    <div id="uxEditFieldMain" class="input-small popup-tools-main field-tools-main">
      <label id="uxEditFieldInfo" class="info-text">You may change any of the following attributes. Click Edit Shape to edit the geometry.</label>
      <ul>
        <li>
          <label id="uxEditFieldFieldNameInfo" class="left-column">ID (required):</label>
          <input id="uxEditFieldFieldName" type="text" data-type="text" maxlength="15" class="right-side"
            onchange="TrimStart(this);ImposeMaxLength(this, 15);" onblur="TrimInput(this);this.onchange();" onkeyup="this.onchange();"
            onkeypress="this.onchange();" />
          <span class="text-small">&nbsp;<span id="uxEditFieldFieldNameCount">15</span><span> characters remaining</span></span>
        </li>
        <li id="uxHoverInfo" class="display-none">
          <label id="uxHoverInfoRight" class=" "></label>
        </li>
        <li>
          <label id="uxEditFieldNotesInfo" class="left-column">
            Notes:
          <br />
            <span class="text-small"><span id="uxEditFieldNotesCount">100</span><span> characters remaining</span></span></label>
          <textarea id="uxEditFieldNotes" class="right-side" name="text" cols="34" rows="3"
            onchange="TrimStart(this);ImposeMaxLength(this, 100);" onblur="TrimInput(this);this.onchange();" onkeyup="this.onchange();"
            onkeypress="this.onchange();"></textarea>
          <br />
        </li>
      </ul>
      <div id="uxEditFieldAccordionAttrs" class="accord-group notaccordion collapsible collapsed clear display-none">
        <h3 id="uxEditFieldsHeaderAttrs" class="accord-header-items">
          <span>More attributes </span>
        </h3>
        <div class="tan">
          <ul>
            <li>
              <label id="uxEditFieldWatershedCodeInfo" class="left-column">12-digit Watershed:</label>
              <input id="uxEditFieldWatershedCode" type="text" data-type="text" maxlength="12" class="right-side"
                onblur="ExtractNumber(this,0,false);" onkeyup="ExtractNumber(this,0,false);" onkeypress="return BlockNonNumbers(event, this, false, false);" />
            </li>
            <li>
              <label id="uxEditFieldFsaFarmNumInfo" class="left-column">FSA Farm number:</label>
              <input id="uxEditFieldFsaFarmNum" type="text" data-type="number" maxlength="5" class="right-side"
                onblur="ExtractNumber(this,0,false);" onkeyup="ExtractNumber(this,0,false);" onkeypress="return BlockNonNumbers(event, this, false, false);" />
            </li>
            <li>
              <label id="uxEditFieldFsaTractNumInfo" class="left-column">FSA Tract number:</label>
              <input id="uxEditFieldFsaTractNum" type="text" data-type="number" maxlength="10" class="right-side"
                onblur="ExtractNumber(this,0,false);" onkeyup="ExtractNumber(this,0,false);" onkeypress="return BlockNonNumbers(event, this, false, false);" />
            </li>
            <li>
              <label id="uxEditFieldFsaFieldNumInfo" class="left-column">FSA Field number:</label>
              <input id="uxEditFieldFsaFieldNum" type="text" data-type="number" maxlength="4" class="right-side"
                onblur="ExtractNumber(this,0,false);" onkeyup="ExtractNumber(this,0,false);" onkeypress="return BlockNonNumbers(event, this, false, false);" />
            </li>
          </ul>
        </div>
      </div>
    </div>
    <div id="uxEditFieldToolsButtonsContainer" class="field-tools-buttons-container center">
      <table id="uxEditFieldToolsButtons" class="full-width">
        <tbody>
          <tr>
            <td id="uxEditFieldToolsButtonsLeft">
              <input type="button" id="uxEditFieldDrawStart" value="Edit Shape" class="margin-small-hori"
                onclick="if (StartDrawing(this)) { GoToMap(); }" title="Edit the terrace area's geometry" data-form-button="start-drawing" />
              <input type="button" id="uxEditFieldDrawDeleteLast" value="Delete Last Pt" class="visibility-none margin-small-hori"
                onclick="DeleteLastDrawnPoint();" title="Delete the last drawn point" data-form-button="del-last-pt" />
              <input type="button" id="uxEditFieldDrawDeleteAll" value="Delete All Pts" class="visibility-none margin-small-hori"
                onclick="DeleteAllDrawnPoints();" title="Delete all points shown" data-form-button="del-all-pts" />
              <input type="button" id="uxEditFieldDrawSubmit" value="Submit" class="margin-small-hori"
                onclick="SubmitFeature();" title="Submit the edited terrace area" data-form-button="submit" />
            </td>
            <td id="uxEditFieldToolsButtonsRight"></td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
            {{/if}}
          </script>
        </div>

        <script type="text/javascript" id="uxFieldsTemplateScript">
          var fieldsTmpl = $.templates("#fieldsTmpl");
          var editFieldsTmpl = $.templates("#editFieldsTmpl");
        </script>

        <div id="uxHighPointContainer" data-view="highPoint gis"></div>
        <%-- render with templating --%>

        <script id="highPointsTmpl" type="text/x-jsrender">
          <div id="uxHighPointsContainer" data-view="highPoint gis" class="">
            <div class="clear-fix">
              <span class="text-center list-title">High Point</span>
              <span class="right">
                <input type="button" value="Refresh" class="accord-button" title="Reload high point from server" onclick="highPointsRetrievedIndx = -99; ReloadHighPoints(this); return false;" />
                <input type="button" value="Close" class="accord-button" title="Return to map view" onclick="$('#uxToggleMapView').trigger('click'); return false;" />
              </span>
            </div>
            <div><span id="uxHighPointsInfo">You do not have a high point created. Use the tools button to create a new high point.</span></div>
            {^{for highPoints}}
            <div id="uxHighPointAccordion{{:#index}}" class="accord-group notaccordion collapsible collapsed">
              <h3 id="uxHighPointsHeader{{:#index}}" class="accord-header-items">
                <input type="hidden" id="uxHighPointOid{{:#index}}" value="{{:highPointRecord.ObjectID}}" />
                <input type="hidden" id="uxHighPointGuid{{:#index}}" value="{{:datumRecord.GUID}}" />
                <input type="radio" name="HighPointSelect" id="uxHighPointSelect{{:#index}}" class="accord-sel" />
                <span class="accord-header-separate-6">
                  <span>High Point: </span><span></span>
                  <span>Lat: </span><span class="text-right">{{LatLng:highPointRecord.Latitude}}</span>
                  <span>Lng: </span><span class="text-right">{{LatLng:highPointRecord.Longitude}}</span></span>
              </h3>
              <div>
                <table id="uxSelectedHighPointDetails{{:#index}}" class="highPoint-table full-width">
                  <tbody>
                    <tr>
                      <td>Latitude: </td>
                      <td>
                        <label id="uxHighPointLatitude{{:#index}}">{{LatLng:highPointRecord.Latitude}}</label></td>
                      <td>Longitude: </td>
                      <td>
                        <label id="uxHighPointLongitude{{:#index}}">{{LatLng:highPointRecord.Longitude}}</label></td>
                    </tr>
                    <tr class="display-none">
                      <td>Elevation: </td>
                      <td>
                        <label id="uxHighPointElevation{{:#index}}">{{positive:highPointRecord.Elevation}}</label></td>
                      <td></td>
                    </tr>
                    <tr>
                    </tr>
                    <tr class="clear">
                      <td class="no-align">Created:
                        <label id="uxHighPointsCreated{{:#index}}">{{SDate datumRecord.Created /}}</label></td>
                      <td class="no-align">Edited:
                        <label id="uxHighPointsEdited{{:#index}}">{{SDate datumRecord.Edited /}}</label></td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
            {{/for}}
          </div>
        </script>

        <div id="uxEditHighPointContainer" class="display-none popup-tools draggable highPoint-tools">
          <script id="editHighPointsTmpl" type="text/x-jsrender">
            {^{if selectedID && selectedID !== '0'}}
  <div id="uxEditHighPointForm" class="popup--form">
    <div id="uxEditHighPointHeader" class="popup-tools-header">
      <h3 id="uxEditHighPointTitle" class="popup--tools-title">Edit High Point</h3>
      <div class="popup-control-panel">
        <img title="This form is draggable when you see the arrows" alt="Draggable form" src="/images/move.png"
          class="control-drag" />
        <img title="Close this form" alt="Close form" src="/images/close.png"
          id="uxEditHighPointDrawCancel" data-form-cancel="highPoint-tools" class="control-close" onclick="CancelHighPointDraw();" />
      </div>
    </div>
    <hr />
    <div id="uxEditHighPointMain" class="input-small popup-tools-main ">
    </div>
    <div id="uxEditHighPointToolsButtonsContainer" class="highPoint-tools-buttons-container center">
      Drag your high point to a new location and hit Submit.
      <table id="uxEditHighPointToolsButtons" class="full-width">
        <tbody>
          <tr>
            <td id="uxEditHighPointToolsButtonsLeft">
              <input type="button" id="uxEditHighPointDrawStart" value="Edit Shape" class="margin-small-hori"
                onclick="if (StartDrawing(this)) { GoToMap(); }" title="Start drawing a new high point" data-form-button="start-drawing" />
              <input type="button" id="uxEditHighPointDrawSubmit" value="Submit" class="margin-small-hori"
                onclick="SubmitFeature();" title="Submit the newly drawn high point" data-form-button="submit" />
            </td>
            <td id="uxEditHighPointToolsButtonsRight"></td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
            {{/if}}
          </script>
        </div>

        <script type="text/javascript" id="uxHighPointsTemplateScript">
          var highPointsTmpl = $.templates("#highPointsTmpl");
          var editHighPointsTmpl = $.templates("#editHighPointsTmpl");
          $.views.converters("LatLng", function (val) { return val.toFixed(6); });
        </script>

        <div id="uxRidgelineContainer" data-view="ridgeline gis"></div>
        <%-- render with templating --%>

        <script id="ridgelinesTmpl" type="text/x-jsrender">
          <div id="uxRidgelinesContainer" data-view="ridgeline gis" class="">
            <div class="clear-fix">
              <span class="text-center list-title">Ridgeline</span>
              <span class="right">
                <input type="button" value="Refresh" class="accord-button" title="Reload ridgeline from server" onclick="ridgelinesRetrievedIndx = -99; ReloadRidgelines(this); return false;" />
                <input type="button" value="Close" class="accord-button" title="Return to map view" onclick="$('#uxToggleMapView').trigger('click'); return false;" />
              </span>
            </div>
            <div><span id="uxRidgelinesInfo">You do not have a ridgeline created. Use the tools button to create a new ridgeline.</span></div>
            {^{for ridgelines}}
            <div id="uxRidgelineAccordion{{:#index}}" class="accord-group notaccordion collapsible collapsed">
              <h3 id="uxRidgelinesHeader{{:#index}}" class="accord-header-items">
                <input type="hidden" id="uxRidgelineOid{{:#index}}" value="{{:ridgelineRecord.ObjectID}}" />
                <input type="hidden" id="uxRidgelineGuid{{:#index}}" value="{{:datumRecord.GUID}}" />
                <input type="radio" name="RidgelineSelect" id="uxRidgelineSelect{{:#index}}" class="accord-sel" />
                <span class="accord-header-separate-6">
                  <span>Ridgeline: </span><span></span>
                  <span>Length: </span><span class="text-right">{{Fix2:ridgelineRecord.Length}}</span></span>
              </h3>
              <div>
                <table id="uxSelectedRidgelineDetails{{:#index}}" class="ridgeline-table full-width">
                  <tbody>
                    <tr>
                    </tr>
                    <tr class="clear">
                      <td class="no-align">Created:
                        <label id="uxRidgelinesCreated{{:#index}}">{{SDate datumRecord.Created /}}</label></td>
                      <td class="no-align">Edited:
                        <label id="uxRidgelinesEdited{{:#index}}">{{SDate datumRecord.Edited /}}</label></td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
            {{/for}}
          </div>
        </script>

        <div id="uxEditRidgelineContainer" class="display-none popup-tools draggable ridgeline-tools">
          <script id="editRidgelinesTmpl" type="text/x-jsrender">
            {^{if selectedID && selectedID !== '0'}}
  <div id="uxEditRidgelineForm" class="popup--form">
    <div id="uxEditRidgelineHeader" class="popup-tools-header">
      <h3 id="uxEditRidgelineTitle" class="popup--tools-title">Edit Ridgeline</h3>
      <div class="popup-control-panel">
        <img title="This form is draggable when you see the arrows" alt="Draggable form" src="/images/move.png"
          class="control-drag" />
        <img title="Close this form" alt="Close form" src="/images/close.png"
          id="uxEditRidgelineDrawCancel" data-form-cancel="ridgeline-tools" class="control-close" onclick="CancelRidgelineDraw();" />
      </div>
    </div>
    <hr />
    <div id="uxEditRidgelineMain" class="input-small popup-tools-main ridgeline-tools-main">
    </div>
    <div id="uxEditRidgelineToolsButtonsContainer" class="ridgeline-tools-buttons-container center">
      Drag points to new locations and hit Submit.
      <table id="uxEditRidgelineToolsButtons" class="full-width">
        <tbody>
          <tr>
            <td id="uxEditRidgelineToolsButtonsLeft">
              <input type="button" id="uxEditRidgelineDrawStart2" value="Edit Shape" class=""
                onclick="actionType = actionTypes.EDIT; StartDrawing(this);" title="Edit the ridgeline's geometry"
                data-form-button="start-drawing" />
              <input type="button" id="uxEditRidgelineDrawStart" value="Edit Shape" class="margin-small-hori"
                onclick="if (StartDrawing(this)) { GoToMap(); }" title="Edit the ridgeline's geometry" data-form-button="start-drawing" />
              <input type="button" id="uxEditRidgelineDrawSubmit" value="Submit" class="margin-small-hori"
                onclick="SubmitFeature();" title="Submit the newly drawn ridgeline" data-form-button="submit" />
            </td>
            <td id="uxEditRidgelineToolsButtonsRight"></td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
            {{/if}}
          </script>
        </div>

        <script type="text/javascript" id="uxRidgelinesTemplateScript">
          var ridgelinesTmpl = $.templates("#ridgelinesTmpl");
          var editRidgelinesTmpl = $.templates("#editRidgelinesTmpl");
          $.views.converters("Fix2", function (val) { return val.toFixed(2); });
        </script>

        <div id="uxDivideContainer" data-view="divide gis"></div>
        <%-- render with templating --%>

        <script id="dividesTmpl" type="text/x-jsrender">
          <div id="uxDividesContainer" data-view="divide gis" class="">
            <div class="clear-fix">
              <span class="text-center list-title">Divide</span>
              <span class="right">
                <input type="button" value="Refresh" class="accord-button" title="Reload divide from server" onclick="dividesRetrievedIndx = -99; ReloadDivides(this); return false;" />
                <input type="button" value="Close" class="accord-button" title="Return to map view" onclick="$('#uxToggleMapView').trigger('click'); return false;" />
              </span>
            </div>
            <div><span id="uxDividesInfo">You do not have a divide created. Use the tools button to create a new divide.</span></div>
            {^{for divides}}
            <div id="uxDivideAccordion{{:#index}}" class="accord-group notaccordion collapsible collapsed">
              <h3 id="uxDividesHeader{{:#index}}" class="accord-header-items">
                <input type="hidden" id="uxDivideOid{{:#index}}" value="{{:divideRecord.ObjectID}}" />
                <input type="hidden" id="uxDivideGuid{{:#index}}" value="{{:datumRecord.GUID}}" />
                <input type="radio" name="DivideSelect" id="uxDivideSelect{{:#index}}" class="accord-sel" />
                <span class="accord-header-separate-6">
                  <span>Divide: </span><span></span>
                  <span>Length: </span><span class="text-right">{{Fix2:divideRecord.Length}}</span></span>
              </h3>
              <div>
                <table id="uxSelectedDivideDetails{{:#index}}" class="divide-table full-width">
                  <tbody>
                    <tr>
                    </tr>
                    <tr class="clear">
                      <td class="no-align">Created:
                        <label id="uxDividesCreated{{:#index}}">{{SDate datumRecord.Created /}}</label></td>
                      <td class="no-align">Edited:
                        <label id="uxDividesEdited{{:#index}}">{{SDate datumRecord.Edited /}}</label></td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
            {{/for}}
          </div>
        </script>

        <div id="uxEditDivideContainer" class="display-none popup-tools draggable divide-tools">
          <script id="editDividesTmpl" type="text/x-jsrender">
            {^{if selectedID && selectedID !== '0'}}
  <div id="uxEditDivideForm" class="popup--form">
    <div id="uxEditDivideHeader" class="popup-tools-header">
      <h3 id="uxEditDivideTitle" class="popup--tools-title">Edit Divide</h3>
      <div class="popup-control-panel">
        <img title="This form is draggable when you see the arrows" alt="Draggable form" src="/images/move.png"
          class="control-drag" />
        <img title="Close this form" alt="Close form" src="/images/close.png"
          id="uxEditDivideDrawCancel" data-form-cancel="divide-tools" class="control-close" onclick="CancelDivideDraw();" />
      </div>
    </div>
    <hr />
    <div id="uxEditDivideMain" class="input-small popup-tools-main divide-tools-main">
    </div>
    <div id="uxEditDivideToolsButtonsContainer" class="divide-tools-buttons-container center">
      Drag points to new locations and hit Submit.
      <table id="uxEditDivideToolsButtons" class="full-width">
        <tbody>
          <tr>
            <td id="uxEditDivideToolsButtonsLeft">
              <input type="button" id="uxEditDivideDrawStart2" value="Edit Shape" class=""
                onclick="actionType = actionTypes.EDIT; StartDrawing(this);" title="Edit the divide's geometry"
                data-form-button="start-drawing" />
              <input type="button" id="uxEditDivideDrawStart" value="Edit Shape" class="margin-small-hori"
                onclick="if (StartDrawing(this)) { GoToMap(); }" title="Edit the divide's geometry" data-form-button="start-drawing" />
              <input type="button" id="uxEditDivideDrawSubmit" value="Submit" class="margin-small-hori"
                onclick="SubmitFeature();" title="Submit the newly drawn divide" data-form-button="submit" />
            </td>
            <td id="uxEditDivideToolsButtonsRight"></td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
            {{/if}}
          </script>
        </div>

        <script type="text/javascript" id="uxDividesTemplateScript">
          var dividesTmpl = $.templates("#dividesTmpl");
          var editDividesTmpl = $.templates("#editDividesTmpl");
          $.views.converters("Fix2", function (val) { return val.toFixed(2); });
        </script>

        <div id="uxWaterwayContainer" data-view="waterway gis"></div>
        <%-- render with templating --%>

        <script id="waterwaysTmpl" type="text/x-jsrender">
          <div id="uxWaterwaysContainer" data-view="waterway gis" class="">
            <div class="clear-fix">
              <span class="text-center list-title">Waterway</span>
              <span class="right">
                <input type="button" value="Refresh" class="accord-button" title="Reload waterway from server" onclick="waterwaysRetrievedIndx = -99; ReloadWaterways(this); return false;" />
                <input type="button" value="Close" class="accord-button" title="Return to map view" onclick="$('#uxToggleMapView').trigger('click'); return false;" />
              </span>
            </div>
            <div><span id="uxWaterwaysInfo">You do not have a waterway created. Use the tools button to create a new waterway.</span></div>
            {^{for waterways}}
            <div id="uxWaterwayAccordion{{:#index}}" class="accord-group notaccordion collapsible collapsed">
              <h3 id="uxWaterwaysHeader{{:#index}}" class="accord-header-items">
                <input type="hidden" id="uxWaterwayOid{{:#index}}" value="{{:waterwayRecord.ObjectID}}" />
                <input type="hidden" id="uxWaterwayGuid{{:#index}}" value="{{:datumRecord.GUID}}" />
                <input type="radio" name="WaterwaySelect" id="uxWaterwaySelect{{:#index}}" class="accord-sel" />
                <span class="accord-header-separate-6">
                  <span>Waterway: </span><span></span>
                  <span>Length: </span><span class="text-right">{{Fix2:waterwayRecord.Length}}</span></span>
              </h3>
              <div>
                <table id="uxSelectedWaterwayDetails{{:#index}}" class="waterway-table full-width">
                  <tbody>
                    <tr>
                    </tr>
                    <tr class="clear">
                      <td class="no-align">Created:
                        <label id="uxWaterwaysCreated{{:#index}}">{{SDate datumRecord.Created /}}</label></td>
                      <td class="no-align">Edited:
                        <label id="uxWaterwaysEdited{{:#index}}">{{SDate datumRecord.Edited /}}</label></td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
            {{/for}}
          </div>
        </script>

        <div id="uxEditWaterwayContainer" class="display-none popup-tools draggable waterway-tools">
          <script id="editWaterwaysTmpl" type="text/x-jsrender">
            {^{if selectedID && selectedID !== '0'}}
  <div id="uxEditWaterwayForm" class="popup--form">
    <div id="uxEditWaterwayHeader" class="popup-tools-header">
      <h3 id="uxEditWaterwayTitle" class="popup--tools-title">Edit Waterway</h3>
      <div class="popup-control-panel">
        <img title="This form is draggable when you see the arrows" alt="Draggable form" src="/images/move.png"
          class="control-drag" />
        <img title="Close this form" alt="Close form" src="/images/close.png"
          id="uxEditWaterwayDrawCancel" data-form-cancel="waterway-tools" class="control-close" onclick="CancelWaterwayDraw();" />
      </div>
    </div>
    <hr />
    <div id="uxEditWaterwayMain" class="input-small popup-tools-main waterway-tools-main">
    </div>
    <div id="uxEditWaterwayToolsButtonsContainer" class="waterway-tools-buttons-container center">
      Drag points to new locations and hit Submit.
      <table id="uxEditWaterwayToolsButtons" class="full-width">
        <tbody>
          <tr>
            <td id="uxEditWaterwayToolsButtonsLeft">
              <input type="button" id="uxEditWaterwayDrawStart2" value="Edit Shape" class=""
                onclick="actionType = actionTypes.EDIT; StartDrawing(this);" title="Edit the waterway's geometry"
                data-form-button="start-drawing" />
              <input type="button" id="uxEditWaterwayDrawStart" value="Edit Shape" class="margin-small-hori"
                onclick="if (StartDrawing(this)) { GoToMap(); }" title="Edit the waterway's geometry" data-form-button="start-drawing" />
              <input type="button" id="uxEditWaterwayDrawSubmit" value="Submit" class="margin-small-hori"
                onclick="SubmitFeature();" title="Submit the newly drawn waterway" data-form-button="submit" />
            </td>
            <td id="uxEditWaterwayToolsButtonsRight"></td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
            {{/if}}
          </script>
        </div>

        <script type="text/javascript" id="uxWaterwaysTemplateScript">
          var waterwaysTmpl = $.templates("#waterwaysTmpl");
          var editWaterwaysTmpl = $.templates("#editWaterwaysTmpl");
          $.views.converters("Fix2", function (val) { return val.toFixed(2); });
        </script>

      </div>
      <%--uxPopupCenter--%>
      <%-- </div>--%>
    </div>

    <div id="uxDebuggerContainer" class="display-none">
      <span id="ResultId"></span>
      <div>
        <span>Page load debugger</span>
        <asp:TextBox ID="PageLoadDebuggerInfo" runat="server" TextMode="MultiLine" Width="100%" Height="400px" Visible="true"></asp:TextBox>
      </div>
    </div>

  </div>
  <%-- END "uxContainer": main div to contain inline page elements --%>

  <%-- BEGIN: popup divs area --%>

  <div id="uxGeometryToolsContainer" class="display-none popup-tools draggable">
    <div id="uxGeometryTools">
      <div id="uxGeometryToolsHeader" class="popup-tools-header" title="Drag title area to move form">
        <span id="uxGeometryToolsTitle" class="popup-tools-title">Map Navigation Tools</span>
        <div class="popup-control-panel">
          <img title="This form is draggable when you see the arrows" alt="Draggable form" src="/images/move.png"
            class="control-drag" />
          <img title="Close this form" alt="Close form" src="/images/close.png"
            id="uxGeometryToolsClose" class="control-close" onclick="CancelGeometryTools();" />
        </div>
      </div>
      <hr />
      <div id="uxGeometryToolsMain" class="popup-tools-main">
        <input type="button" class="zoomButton" id="Button2" value="Toggle Soils Layer"
          onclick="showSoilsOverlay = !showSoilsOverlay; ToggleSoils();" runat="server" title="Click to toggle visibility of the soils layer" />
        <div>
          <input type="button" id="Button3" value="Zoom to All Features"
            onclick="SetMapExtentByOids();" title="Click to zoom map to all features for this project" />
          <span class=""></span>
        </div>
      </div>
      <div id="uxGeometryToolsButtonsContainer" class="popup-tools-buttons-container">
      </div>
    </div>
  </div>

  <div id="uxCreateFieldContainer" class="display-none popup-tools draggable field-tools">
    <div id="uxCreateFieldForm" class="popup--form">
      <div id="uxCreateFieldHeader" class="popup-tools-header">
        <h3 id="uxCreateFieldTitle" class="popup--tools-title">Create Terrace Area</h3>
        <div class="popup-control-panel">
          <img title="This form is draggable when you see the arrows" alt="Draggable form" src="/images/move.png"
            class="control-drag" />
          <img title="Close this form" alt="Close form" src="/images/close.png"
            id="uxFieldDrawCancel" data-form-cancel="field-tools" class="control-close" onclick="CancelFieldDraw();" />
        </div>
      </div>
      <hr />
      <div id="uxCreateFieldMain" class="input-small popup-tools-main field-tools-main display-none">
        <label id="uxCreateFieldInfo" class="info-text">Click on the map to add points to your terrace area.</label>
      </div>
      <div id="uxCreateFieldButtonsContainer" class="field-tools-buttons-container center">
        <table id="uxCreateFieldButtons" class="full-width">
          <tbody>
            <tr>
              <td id="uxCreateFieldButtonsLeft">
                <input type="button" id="uxFieldDrawStart" value="Start Drawing" class="margin-small-hori"
                  onclick="if (StartDrawing(this)) { GoToMap(); }" title="Start drawing a new terrace area" data-form-button="start-drawing" />
                <input type="button" id="uxFieldDrawDeleteLast" value="Delete Last Pt" class="visibility-none margin-small-hori"
                  onclick="DeleteLastDrawnPoint();" title="Delete the last drawn point" data-form-button="del-last-pt" />
                <input type="button" id="uxFieldDrawDeleteAll" value="Delete All Pts" class="visibility-none margin-small-hori"
                  onclick="DeleteAllDrawnPoints();" title="Delete all points shown" data-form-button="del-all-pts" />
                <input type="button" id="uxFieldDrawSubmit" value="Submit" class="visibility-none margin-small-hori"
                  onclick="SubmitFeature();" title="Submit the newly drawn terrace area" data-form-button="submit" />
              </td>
              <td id="uxCreateFieldButtonsRight"></td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>

  <div id="uxImportContourContainer" class="display-none popup-container">
    <div id="uxImportContourBackground" class="popup-background"></div>
    <div id="uxImportContourForm" class="popup-form draggable">
      <div id="uxImportContourHeader" class="popup-header">
        <h3>Import Contours</h3>
        <div class="popup-control-panel">
          <img title="This form is draggable when you see the arrows" alt="Draggable form" src="/images/move.png"
            class="control-drag" />
          <img title="Close this form" alt="Close form" src="/images/close.png"
            data-form-cancel="contour-tools" class="control-close" onclick="CancelImportContourRaws();" />
        </div>
      </div>
      <hr />
      <div id="uxImportContourMain" class="input-small popup-tools-main contour-tools-main">
        <asp:HiddenField ID="uxHiddenFileUpload" runat="server" />
        <input type="hidden" id="uxHiddenImportContourFileName" />
        <div data-import="select" class="">
          <ul>
            <li>
              <label id="uxImportContourInfo" class="info-text">
                Select a file using the Browse button, 
            then click the Upload button. The file must be a .zip file containing only the 
            shapefile you want imported. After it is uploaded, you will be prompted for more information.
              </label>
            </li>
            <li class="whitespace">
              <label id="uxImportContourContourNameInfo" class="left-column">File (.zip only):</label>
              <input type="file" name="uxImportContourGisFile" id="uxImportContourGisFile" onchange="FileSelected();"
                onkeydown="return (event.keyCode!=13);" accept="application/zip" />
              <div id="details"></div>
              <div>
              </div>
              <div id="progress"></div>
            </li>
          </ul>
        </div>

        <div data-import="columns" class="display-none">
          <ul>
            <li class="text-center whitespace">
              <label id="uxImportContourRecordCount"></label>
            </li>
            <li>
              <label>Verify the column to be used for the contour elevation.</label>
            </li>
            <li>
              <label id="uxImportContourContourColumnInfo" class="left-column">Contour column:</label>
              <select id="uxImportContourContourColumn">
                <option value="-1">-none-</option>
              </select>
            </li>
          </ul>
        </div>

        <div data-import="import" class="display-none">
          <ul>
            <li>Click the Import button to add the shapefile records to your contours.
            </li>
          </ul>
        </div>
      </div>
      <div id="uxImportContourButtons" class="popup-footer">
        <input type="button" onclick="UploadFile()" value="Upload File" title="Upload the selected file to the server" />
        <input type="button" id="uxImportContourImport" onclick="VerifyImportContourRaws(this, 'GIS');" value="Import Contours" title="Import contours to project" disabled />
        <input type="button" id="uxImportContourCancel" onclick="CancelImportContourRaws();" title="Cancel contour import and close this form" value="Cancel" />
      </div>
    </div>
  </div>

  <div id="uxCreateHighPointContainer" class="display-none popup-tools draggable highPoint-tools">
    <div id="uxCreateHighPointForm" class="popup--form">
      <div id="uxCreateHighPointHeader" class="popup-tools-header">
        <h3 id="uxCreateHighPointTitle" class="popup--tools-title">Create High Point</h3>
        <div class="popup-control-panel">
          <img title="This form is draggable when you see the arrows" alt="Draggable form" src="/images/move.png"
            class="control-drag" />
          <img title="Close this form" alt="Close form" src="/images/close.png"
            id="uxHighPointDrawCancel" data-form-cancel="highPoint-tools" class="control-close" onclick="CancelHighPointDraw();" />
        </div>
      </div>
      <hr />
      <div id="uxCreateHighPointMain" class="input-small popup-tools-main highPoint-tools-main">
        <label id="uxCreateHighPointInfo" class="info-text">Click on the map at the location of your maximum elevation.</label>
      </div>
      <div id="uxCreateHighPointButtonsContainer" class="highPoint-tools-buttons-container center">
        <table id="uxCreateHighPointButtons" class="full-width">
          <tbody>
            <tr>
              <td id="uxCreateHighPointButtonsLeft">
                <input type="button" id="uxHighPointDrawStart" value="Start Drawing" class="margin-small-hori"
                  onclick="if (StartDrawing(this)) { GoToMap(); }" title="Start drawing a new high point" data-form-button="start-drawing" />
                <input type="button" id="uxHighPointDrawSubmit" value="Submit" class="visibility-none margin-small-hori"
                  onclick="SubmitFeature();" title="Submit the newly drawn high point" data-form-button="submit" />
              </td>
              <td id="uxCreateHighPointButtonsRight"></td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>

  <div id="uxCreateRidgelineContainer" class="display-none popup-tools draggable ridgeline-tools">
    <div id="uxCreateRidgelineForm" class="popup--form">
      <div id="uxCreateRidgelineHeader" class="popup-tools-header">
        <h3 id="uxCreateRidgelineTitle" class="popup--tools-title">Create Ridgeline</h3>
        <div class="popup-control-panel">
          <img title="This form is draggable when you see the arrows" alt="Draggable form" src="/images/move.png"
            class="control-drag" />
          <img title="Close this form" alt="Close form" src="/images/close.png"
            id="uxRidgelineDrawCancel" data-form-cancel="ridgeline-tools" class="control-close" onclick="CancelRidgelineDraw();" />
        </div>
      </div>
      <hr />
      <div id="uxCreateRidgelineMain" class="input-small popup-tools-main ridgeline-tools-main">
        <label id="uxCreateRidgelineInfo" class="info-text">Click on the map to add points to the ridgeline.</label>
      </div>
      <div id="uxCreateRidgelineButtonsContainer" class="ridgeline-tools-buttons-container center">
        <table id="uxCreateRidgelineButtons" class="full-width">
          <tbody>
            <tr>
              <td id="uxCreateRidgelineButtonsLeft">
                <input type="button" id="uxRidgelineDrawStart" value="Start Drawing" class="margin-small-hori"
                  onclick="if (StartDrawing(this)) { GoToMap(); }" title="Start drawing a ridgeline" data-form-button="start-drawing" />
                <input type="button" id="uxRidgelineDrawSubmit" value="Submit" class="visibility-none margin-small-hori"
                  onclick="SubmitFeature();" title="Submit the newly drawn ridgeline" data-form-button="submit" />
              </td>
              <td id="uxCreateRidgelineButtonsRight"></td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>

  <div id="uxCreateDivideContainer" class="display-none popup-tools draggable divide-tools">
    <div id="uxCreateDivideForm" class="popup--form">
      <div id="uxCreateDivideHeader" class="popup-tools-header">
        <h3 id="uxCreateDivideTitle" class="popup--tools-title">Create Divide</h3>
        <div class="popup-control-panel">
          <img title="This form is draggable when you see the arrows" alt="Draggable form" src="/images/move.png"
            class="control-drag" />
          <img title="Close this form" alt="Close form" src="/images/close.png"
            id="uxDivideDrawCancel" data-form-cancel="divide-tools" class="control-close" onclick="CancelDivideDraw();" />
        </div>
      </div>
      <hr />
      <div id="uxCreateDivideMain" class="input-small popup-tools-main divide-tools-main">
        <label id="uxCreateDivideInfo" class="info-text">
          Please enter the order index for the feature, 
          then click Start Drawing to add the divide to the map. Setting the index to a lower number will cause
          existing indices higher than the entered number to be incremented.</label>
        <ul>
          <li>
            <label id="uxCreateDivideOrdinalInfo" class="left-column">&nbsp;</label>
            <span class="right-side">
              <input type="button" class="arrow-button" value="<" onclick="DecrementDivideIndex('uxCreateDivideOrdinal');"
                title="Click to decrement the index for this feature." />
              <input id="uxCreateDivideOrdinal" type="text" data-type="text" disabled />
              <input type="button" class="arrow-button" value=">" onclick="IncrementDivideIndex('uxCreateDivideOrdinal');"
                title="Click to increment the index for this feature." />
              <label id="uxMaxDivideOrdinalInfo">(max of )</label>
            </span>
          </li>
        </ul>
      </div>
      <div id="uxCreateDivideButtonsContainer" class="divide-tools-buttons-container center">
        <table id="uxCreateDivideButtons" class="full-width">
          <tbody>
            <tr>
              <td id="uxCreateDivideButtonsLeft">
                <input type="button" id="uxDivideDrawStart" value="Start Drawing" class="margin-small-hori"
                  onclick="if (StartDrawing(this)) { GoToMap(); }" title="Start drawing a divide" data-form-button="start-drawing" />
                <input type="button" id="uxDivideDrawDeleteLast" value="Delete Last Pt" class="visibility-none margin-small-hori"
                  onclick="DeleteLastDrawnPoint();" title="Delete the last drawn point" data-form-button="del-last-pt" />
                <input type="button" id="uxDivideDrawDeleteAll" value="Delete All Pts" class="visibility-none margin-small-hori"
                  onclick="DeleteAllDrawnPoints();" title="Delete all points shown" data-form-button="del-all-pts" />
                <input type="button" id="uxDivideDrawSubmit" value="Submit" class="margin-small-hori"
                  onclick="SubmitFeature();" title="Submit the newly drawn divide" data-form-button="submit" />
              </td>
              <td id="uxCreateDivideButtonsRight"></td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>

  <div id="uxOrderDivideContainer" class="display-none popup-tools draggable divide-tools">
    <div id="uxOrderDivideForm" class="popup--form">
      <div id="uxOrderDivideHeader" class="popup-tools-header">
        <h3 id="uxOrderDivideTitle" class="popup--tools-title">Order Divides</h3>
        <div class="popup-control-panel">
          <img title="This form is draggable when you see the arrows" alt="Draggable form" src="/images/move.png"
            class="control-drag" />
          <img title="Close this form" alt="Close form" src="/images/close.png"
            id="uxOrderDivideDrawCancel" class="control-close" onclick="CancelOrderDivideTool();" />
        </div>
      </div>
      <hr />
      <div id="uxOrderDivideMain" class="input-small popup-tools-main divide-tools-main">
        <label id="uxOrderDivideInfo" class="info-text">
          Highlight a feature and adjust as needed.
          The decrement (<) and increment (>) buttons will cause an index 'swap' with the feature that has the de/incremented index.
          No changes are saved until submitted.
        </label>
        <ul id="uxOrderDivideList">
          <li data-sample="This is sample entry, gets deleted when opening form.">
            <input type="hidden" data-field="ObjectID" value="4265" />
            <span class="left-column">
              <input type="button" onclick="myDivides.HighlightFeature(4265);" value="Highlight" />
            </span>
            <span class="right-side">
              <input type="button" class="arrow-button" value="<" onclick="SwapDivideIndex(this, -1);"
                title="Click to decrement the index for this feature." />
              <input type="text" data-type="text" data-field="Ordinal" value="2" />
              <input type="button" class="arrow-button" value=">" onclick="SwapDivideIndex(this, 1);"
                title="Click to increment the index for this feature." />
            </span>
          </li>
        </ul>
      </div>
      <div id="uxOrderDivideButtonsContainer" class="divide-tools-buttons-container text-center">
        <input type="button" value="Submit" class="margin-small-hori"
          onclick="SubmitOrderDivideTool();" title="Submit changes to database" />
        <input type="button" value="Close" class="margin-small-hori"
          onclick="CancelOrderDivideTool();" title="Close form, keeping original values" />
      </div>
      <div id="uxOrderDivideWarning2">
        <label id="uxOrderDivideWarning"></label>
      </div>
    </div>
  </div>

  <div id="uxAlignDivideContainer" class="display-none popup-tools draggable divide-tools">
    <div id="uxAlignDivideForm" class="popup--form">
      <div id="uxAlignDivideHeader" class="popup-tools-header">
        <h3 id="uxAlignDivideTitle" class="popup--tools-title">Align Divides</h3>
        <div class="popup-control-panel">
          <img title="This form is draggable when you see the arrows" alt="Draggable form" src="/images/move.png"
            class="control-drag" />
          <img title="Close this form" alt="Close form" src="/images/close.png"
            id="uxAlignDivideDrawCancel" class="control-close" onclick="CancelAlignDivideTool();" />
        </div>
      </div>
      <hr />
      <div id="uxAlignDivideMain" class="input-small popup-tools-main divide-tools-main">
        <label id="uxAlignDivideInfo" class="info-text">
          Highlight a feature and adjust as needed.
          The markers on the map indicate the first coordinates of the divides. All divides should go in the same direction.
          No changes are saved until submitted.
        </label>
        <ul id="uxAlignDivideList">
          <li data-sample="This is sample entry, gets deleted when opening form.">
            <input type="hidden" data-field="ObjectID" value="4265" />
            <span class="left-column">
              <input type="button" onclick="myDivides.HighlightFeature(4265);" value="Highlight" />
            </span>
            <span class="right-side">
              <input type="button" value="Reverse" onclick="ReverseDivide(4265);"
                title="Click to reverse the coordinate order for this feature." />
              <label data-warning="geometry" class="display-none">No geometry</label>
              <input type="hidden" data-field="Coords" value="3,4 5,9" />
            </span>
          </li>
        </ul>
      </div>
      <div id="uxAlignDivideButtonsContainer" class="divide-tools-buttons-container text-center">
        <input type="button" value="Submit" class="margin-small-hori"
          onclick="SubmitAlignDivideTool();" title="Submit changes to database and close this form." />
        <input type="button" value="Close" class="margin-small-hori"
          onclick="CancelAlignDivideTool();" title="Close form, keeping original values" />
      </div>
      <div id="uxAlignDivideWarning2">
        <label id="uxAlignDivideWarning"></label>
      </div>
    </div>
  </div>

  <div id="uxCreateWaterwayContainer" class="display-none popup-tools draggable waterway-tools">
    <div id="uxCreateWaterwayForm" class="popup--form">
      <div id="uxCreateWaterwayHeader" class="popup-tools-header">
        <h3 id="uxCreateWaterwayTitle" class="popup--tools-title">Create Waterway</h3>
        <div class="popup-control-panel">
          <img title="This form is draggable when you see the arrows" alt="Draggable form" src="/images/move.png"
            class="control-drag" />
          <img title="Close this form" alt="Close form" src="/images/close.png"
            id="uxWaterwayDrawCancel" data-form-cancel="waterway-tools" class="control-close" onclick="CancelWaterwayDraw();" />
        </div>
      </div>
      <hr />
      <div id="uxCreateWaterwayMain" class="input-small popup-tools-main waterway-tools-main">
        <label id="uxCreateWaterwayInfo" class="info-text">
          Please enter the order index for the feature, 
          then click Start Drawing to add the waterway to the map. Setting the index to a lower number will cause
          existing indices higher than the entered number to be incremented.</label>
        <ul>
          <li>
            <label id="uxCreateWaterwayOrdinalInfo" class="left-column">&nbsp;</label>
            <span class="right-side">
              <input type="button" class="arrow-button" value="<" onclick="DecrementWaterwayIndex('uxCreateWaterwayOrdinal');"
                title="Click to decrement the index for this feature." />
              <input id="uxCreateWaterwayOrdinal" type="text" data-type="text" disabled />
              <input type="button" class="arrow-button" value=">" onclick="IncrementWaterwayIndex('uxCreateWaterwayOrdinal');"
                title="Click to increment the index for this feature." />
              <label id="uxMaxWaterwayOrdinalInfo">(max of )</label>
            </span>
          </li>
        </ul>
      </div>
      <div id="uxCreateWaterwayButtonsContainer" class="waterway-tools-buttons-container center">
        <table id="uxCreateWaterwayButtons" class="full-width">
          <tbody>
            <tr>
              <td id="uxCreateWaterwayButtonsLeft">
                <input type="button" id="uxWaterwayDrawStart" value="Start Drawing" class="margin-small-hori"
                  onclick="if (StartDrawing(this)) { GoToMap(); }" title="Start drawing a waterway" data-form-button="start-drawing" />
                <input type="button" id="uxWaterwayDrawDeleteLast" value="Delete Last Pt" class="visibility-none margin-small-hori"
                  onclick="DeleteLastDrawnPoint();" title="Delete the last drawn point" data-form-button="del-last-pt" />
                <input type="button" id="uxWaterwayDrawDeleteAll" value="Delete All Pts" class="visibility-none margin-small-hori"
                  onclick="DeleteAllDrawnPoints();" title="Delete all points shown" data-form-button="del-all-pts" />
                <input type="button" id="uxWaterwayDrawSubmit" value="Submit" class="margin-small-hori"
                  onclick="SubmitFeature();" title="Submit the newly drawn waterway" data-form-button="submit" />
              </td>
              <td id="uxCreateWaterwayButtonsRight"></td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>

  <div id="uxOrderWaterwayContainer" class="display-none popup-tools draggable waterway-tools">
    <div id="uxOrderWaterwayForm" class="popup--form">
      <div id="uxOrderWaterwayHeader" class="popup-tools-header">
        <h3 id="uxOrderWaterwayTitle" class="popup--tools-title">Order Waterways</h3>
        <div class="popup-control-panel">
          <img title="This form is draggable when you see the arrows" alt="Draggable form" src="/images/move.png"
            class="control-drag" />
          <img title="Close this form" alt="Close form" src="/images/close.png"
            id="uxOrderWaterwayDrawCancel" class="control-close" onclick="CancelOrderWaterwayTool();" />
        </div>
      </div>
      <hr />
      <div id="uxOrderWaterwayMain" class="input-small popup-tools-main waterway-tools-main">
        <label id="uxOrderWaterwayInfo" class="info-text">
          Highlight a feature and adjust as needed.
          The decrement (<) and increment (>) buttons will cause an index 'swap' with the feature that has the de/incremented index.
          No changes are saved until submitted.
        </label>
        <ul id="uxOrderWaterwayList">
          <li data-sample="This is sample entry, gets deleted when opening form.">
            <input type="hidden" data-field="ObjectID" value="4265" />
            <span class="left-column">
              <input type="button" onclick="myWaterways.HighlightFeature(4265);" value="Highlight" />
            </span>
            <span class="right-side">
              <input type="button" class="arrow-button" value="<" onclick="SwapWaterwayIndex(this, -1);"
                title="Click to decrement the index for this feature." />
              <input type="text" data-type="text" data-field="Ordinal" value="2" />
              <input type="button" class="arrow-button" value=">" onclick="SwapWaterwayIndex(this, 1);"
                title="Click to increment the index for this feature." />
            </span>
          </li>
        </ul>
      </div>
      <div id="uxOrderWaterwayButtonsContainer" class="waterway-tools-buttons-container text-center">
        <input type="button" value="Submit" class="margin-small-hori"
          onclick="SubmitOrderWaterwayTool();" title="Submit changes to database" />
        <input type="button" value="Close" class="margin-small-hori"
          onclick="CancelOrderWaterwayTool();" title="Close form, keeping original values" />
      </div>
      <div id="uxOrderWaterwayWarning2">
        <label id="uxOrderWaterwayWarning"></label>
      </div>
    </div>
  </div>

  <div id="uxAlignWaterwayContainer" class="display-none popup-tools draggable waterway-tools">
    <div id="uxAlignWaterwayForm" class="popup--form">
      <div id="uxAlignWaterwayHeader" class="popup-tools-header">
        <h3 id="uxAlignWaterwayTitle" class="popup--tools-title">Align Waterways</h3>
        <div class="popup-control-panel">
          <img title="This form is draggable when you see the arrows" alt="Draggable form" src="/images/move.png"
            class="control-drag" />
          <img title="Close this form" alt="Close form" src="/images/close.png"
            id="uxAlignWaterwayDrawCancel" class="control-close" onclick="CancelAlignWaterwayTool();" />
        </div>
      </div>
      <hr />
      <div id="uxAlignWaterwayMain" class="input-small popup-tools-main waterway-tools-main">
        <label id="uxAlignWaterwayInfo" class="info-text">
          Highlight a feature and adjust as needed.
          The markers on the map indicate the first coordinates of the waterways. All waterways should go in the same direction.
          No changes are saved until submitted.
        </label>
        <ul id="uxAlignWaterwayList">
          <li data-sample="This is sample entry, gets deleted when opening form.">
            <input type="hidden" data-field="ObjectID" value="4265" />
            <span class="left-column">
              <input type="button" onclick="myWaterways.HighlightFeature(4265);" value="Highlight" />
            </span>
            <span class="right-side">
              <input type="button" value="Reverse" onclick="ReverseWaterway(4265);"
                title="Click to reverse the coordinate order for this feature." />
              <label data-warning="geometry" class="display-none">No geometry</label>
              <input type="hidden" data-field="Coords" value="3,4 5,9" />
            </span>
          </li>
        </ul>
      </div>
      <div id="uxAlignWaterwayButtonsContainer" class="waterway-tools-buttons-container text-center">
        <input type="button" value="Submit" class="margin-small-hori"
          onclick="SubmitAlignWaterwayTool();" title="Submit changes to database and close this form." />
        <input type="button" value="Close" class="margin-small-hori"
          onclick="CancelAlignWaterwayTool();" title="Close form, keeping original values" />
      </div>
      <div id="uxAlignWaterwayWarning2">
        <label id="uxAlignWaterwayWarning"></label>
      </div>
    </div>
  </div>

  <div id="uxEquipmentContainer" class="display-none popup-tools draggable Equipment-tools">
    <div id="uxEquipmentForm" class="popup--form">
      <div id="uxEquipmentHeader" class="popup-tools-header">
        <h3 id="uxEquipmentTitle" class="popup--tools-title">Equipment</h3>
        <div class="popup-control-panel">
          <img title="This form is draggable when you see the arrows" alt="Draggable form" src="/images/move.png"
            class="control-drag" />
          <img title="Close this form" alt="Close form" src="/images/close.png"
            id="uxEquipmentDrawCancel" class="control-close" onclick="CancelEquipmentTool();" />
        </div>
      </div>
      <hr />
      <div id="uxEquipmentMain" class="input-small popup-tools-main equipment-tools-main">
        <label id="uxEquipmentInfo" class="info-text">
          Change equipment settings as desired. Defaults are 1, 30 and 12 and any blank entries will use those defaults.
        </label>
        <p></p>
        <ul id="uxEquipmentList">
          <li>
            <span class="left-column">Number of Machines:</span>
            <input type="text" class="right-column" id="uxNumberOfMachines" value="1"
              onblur="ExtractNumber(this,0,false);ImposeMaxLength(this, 2);" onkeyup="ExtractNumber(this,0,false);"
              onkeypress="return BlockNonNumbers(event, this, false, false);" />
          </li>
          <li>
            <span class="left-column">Machine Row Width:</span>
            <input type="text" class="right-column" id="uxMachineRowWidth" value="30"
              onblur="ExtractNumber(this,0,false);" onkeyup="ExtractNumber(this,0,false);"
              onkeypress="return BlockNonNumbers(event, this, false, false);" />
            (inches)
          </li>
          <li>
            <span class="left-column">Number of Rows:</span>
            <input type="text" class="right-column" id="uxNumberOfRows" value="12"
              onblur="ExtractNumber(this,0,false);" onkeyup="ExtractNumber(this,0,false);"
              onkeypress="return BlockNonNumbers(event, this, false, false);" />
          </li>
        </ul>
        <ul id="uxOptionsList" class="display-none">
          <li>
            <span class="left-column">Max Channel Velocity</span>
            <input type="text" class="right-column" id="uxMaxChannelVel" />
          </li>
          <li>
            <span class="left-column">Mannings</span>
            <input type="text" class="right-column" id="uxMannings" />
          </li>
          <li>
            <span class="left-column">Side Slope</span>
            <input type="text" class="right-column" id="uxSideslope" />
          </li>
          <li>
            <span class="left-column">Runoff Coefficient</span>
            <input type="text" class="right-column" id="uxRunoffcoeff" />
          </li>
          <li>
            <span class="left-column">Runoff Intensity</span>
            <input type="text" class="right-column" id="uxRunoffIntensity" />
          </li>
          <li>
            <span class="left-column">Bottom Terrace Channel</span>
            <input type="text" class="right-column" id="uxBotterrace_channel" />
          </li>
          <li>
            <span class="left-column">Land Slope</span>
            <input type="text" class="right-column" id="uxLandSlope" />
          </li>
        </ul>
      </div>
      <div id="uxEquipmentButtonsContainer" class="equipment-tools-buttons-container text-center">
        <input type="button" value="Submit" class="margin-small-hori"
          onclick="SubmitEquipmentTool();" title="Submit changes to database and close this form." />
        <input type="button" value="Close" class="margin-small-hori"
          onclick="CancelEquipmentTool();" title="Close form, keeping original values" />
      </div>
      <div id="uxEquipmentWarning2">
        <label id="uxEquipmentWarning"></label>
      </div>
    </div>
  </div>

  <div id="uxTerraceContainer" class="display-none popup-tools draggable terrace-tools">
    <div id="uxTerraceForm" class="popup--form">
      <div id="uxTerraceHeader" class="popup-tools-header">
        <h3 id="uxTerraceTitle" class="popup--tools-title">Terrace Selection</h3>
        <div class="popup-control-panel">
          <img title="This form is draggable when you see the arrows" alt="Draggable form" src="/images/move.png"
            class="control-drag" />
          <img title="Close this form" alt="Close form" src="/images/close.png"
            id="uxTerraceDrawCancel" class="control-close" onclick="CloseForm('uxTerrace');" />
        </div>
      </div>
      <hr />
      <div id="uxTerraceMain" class="input-small popup-tools-main terrace-tools-main notaccordion">
      </div>
      <div id="uxTerraceButtonsContainer" class="terrace-tools-buttons-container text-center">
        <input type="button" value="Submit" class="margin-small-hori"
          onclick="SubmitTerraceTool();" title="Submit changes to database and close this form." />
        <input type="button" value="Close" class="margin-small-hori"
          onclick="CloseForm('uxTerrace');" title="Close form, keeping original values" />
      </div>
    </div>
  </div>

  <%-- END: popup divs area --%>

  <%-- END: non-inline elements --%>

  <script type="text/javascript"> window.onload = function () { initialize(); } </script>
  <script type="text/javascript" src="http://maps.googleapis.com/maps/api/js?v=3&libraries=geometry"></script>
  <script type="text/javascript" src="http://serverapi.arcgisonline.com/jsapi/gmaps/?v=1.6"></script>
  <script type="text/javascript" src="https://cdn.rawgit.com/googlemaps/v3-utility-library/master/infobox/src/infobox_packed.js"></script>
  <script type="text/javascript" src="https://cdn.rawgit.com/printercu/google-maps-utility-library-v3-read-only/master/arcgislink/src/arcgislink_compiled.js"></script>
  <script type="text/javascript" src="/Scripts/ProjectHome.js?v=20150918"></script>
  <script type="text/javascript" src="/Scripts/TerraceArea.js?v=20150918"></script>
  <script type="text/javascript" src="/Scripts/HighPoint.js?v=20150918"></script>
  <script type="text/javascript" src="/Scripts/Ridgeline.js?v=20150918"></script>
  <script type="text/javascript" src="/Scripts/Divide.js?v=20150918"></script>
  <script type="text/javascript" src="/Scripts/Waterway.js?v=20150918"></script>
  <script type="text/javascript" src="/Scripts/Contour.js?v=20150918"></script>
  <script type="text/javascript" src="/Scripts/ContourRaw.js?v=20150918"></script>
  <script type="text/javascript" src="/Scripts/UploadGIS.js?v=20150918"></script>
  <script type="text/javascript" src="/Scripts/Equipment.js?v=20160523"></script>
  <script type="text/javascript" src="/Scripts/Terrace.js?v=20150918"></script>
  <script type="text/javascript" src="/Scripts/polysnapper.js?v=20160314"></script>
</asp:Content>
