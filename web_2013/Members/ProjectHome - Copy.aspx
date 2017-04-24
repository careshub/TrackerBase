  
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="Server"> 

  <div id="uxContainer">
    <%-- main div to contain inline page elements --%>
     
    <div class="col--mask right--menu clear">
      <%-- <div class="col--left">--%>
      <div class="col--2 home-menu">
        <div id="uxMapControls" class="">
          <div id="uxAccordionNav" class="accordion collapsible">
            <h3>Map Controls</h3>
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
                onclick="LoadTerraces();LoadSmoothContours();" runat="server" title="Load terraces from fortran output." />
            </div>
            <h3 class="step-four accord-display-none">Terraces</h3>
            <div id="uxTerraceContainer" class="step-four"></div>
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

        <script id="terracesTmpl" type="text/x-jsrender">
          <input type="button" id="uxOpenReportPage" class="main-menu" value="Open Report Page"
            onclick="OpenReportPage();" runat="server" title="Opens a new tab with report and error info" />
          <br /><label id="uxTerracesInfo"></label>
          <div id="uxTerraceExistsStuff">
            <ul>
              <li>
                <input type="button" id="uxShowTerraces" onclick="myTerraces.Show(); SetToggleTerrace(true);" value="Show All" />
                <input type="button" id="uxHideTerraces" onclick="myTerraces.Hide(); SetToggleTerrace(false);" value="Hide All" />
              </li>
              <li>
                <label>Cost ($/ft): </label>
                <select id="uxTerraceCostPerFtMain" data-select="costPerFt"></select>
                <input type="button" id="uxSetCostPerFt" data-set="costPerFt"
                  onclick="SetTerraceCosts(this, 'uxTerraceCostPerFt');" value="Set All" title="Set all cost per foot selections" />
              </li>
              <li>
                <input type="button" id="uxSaveCostPerFt" data-set="costPerFt"
                  onclick="SaveTerraceCosts(this);" value="Save All" title="Save all cost info to database" /></li>
            </ul>
          </div>
          {^{if selectedID && selectedID !== '0'}}
          {{for terraces}}
          {{for propSet.name}}
              <h3 id="uxTerraceHeader{{:#index}}" class="accord-header-items step-four accord-display-none">
                <input type="hidden" id="uxTerraceOid{{:#index}}" value="{{:terraceRecord.ObjectID}}" />
                <input type="hidden" id="uxTerraceGuid{{:#index}}" value="{{:datumRecord.GUID}}" />
                <span class="">
                  <%--<span>Terrace: </span>--%><span>{{:propSet.name}}</span>
                  <%--<span>Type: </span>--%><span class="text-right">{{:terraceRecord.Type}}</span>
                  {{if !terraceRecord.Shape || terraceRecord.Shape.trim().length<1}}<span class="warning">No Shape</span>{{/if}}</span>
              </h3>
          {{for terraceRecord}}
          <div id="uxTerraceDetails{{:#index}}">
            <ul>
              <li>
                <label>
                  <input type="checkbox" id="uxToggleTerrace{{:#index}}" checked onclick="ToggleTerrace(this);" />
                  Show
                </label>
                <label>
                  <input type="checkbox" id="uxToggleTerraceHighlight{{:#index}}" onclick="ToggleTerraceHighlight(this);" />
                  Highlight
                </label>
              </li>
              <li>
                <label>Cost ($/ft): </label>
                <select id="uxTerraceCostPerFt{{:#index}}" data-select="costPerFt" onchange="SetTerraceCost(this);"></select>
              </li>
              <li>
                <label>Total Cost ($): </label>
                <label id="uxTerraceTotalCost{{:#index}}">0.00</label>
              </li>
            </ul>
          </div>
          {{/for}}
          {{/for}}
          {{/for}}
            {{/if}}
        </script>

        <script type="text/javascript" id="uxTerracesTemplateScript">
          var terracesTmpl = $.templates("#terracesTmpl");
        </script>

      </div>
      <%--uxPopupCenter--%>
      <%-- </div>--%>
    </div>

  </div>
  <%-- END "uxContainer": main div to contain inline page elements --%>

  <%-- BEGIN: popup divs area --%>
   
  <%-- END: popup divs area --%>

  <%-- END: non-inline elements --%>
   
</asp:Content>
