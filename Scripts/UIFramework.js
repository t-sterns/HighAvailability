function DrawLineChart(elementId, data) {

    var svgroot = document.createElementNS ? document.createElementNS("http://www.w3.org/2000/svg", "svg") : document.createElement("svg");
    document.getElementById(elementId).appendChild(svgroot);

    chart = nv.models.lineChart()
                  .useInteractiveGuideline(true)
                  .transitionDuration(500)
                  .showLegend(true)
                  .showYAxis(true)
                  .showXAxis(true);

    chart.xAxis
        .axisLabel('Time')
        .tickFormat(d3.format(',r'));

    chart.yAxis
        .axisLabel('Value')
        .tickFormat(d3.format('.02f'));

    var svgElement = '#' + elementId + ' svg';
    d3.select(svgElement)
         .datum(data)
         .call(chart);

    nv.utils.windowResize(function () { chart.update() });

    nv.addGraph(function () { return chart; });
    return chart;
}

function DrawLineChartWithFocusControl(elementId, data) {

    var svgroot = document.createElementNS ? document.createElementNS("http://www.w3.org/2000/svg", "svg") : document.createElement("svg");
    document.getElementById(elementId).appendChild(svgroot);

    var chart = nv.models.lineWithFocusChart();

    chart.xAxis
        .axisLabel('Time')
        .tickFormat(d3.format(',r'));

    chart.yAxis
        .axisLabel('Value')
        .tickFormat(d3.format('.02f'));

    chart.y2Axis
        .tickFormat(d3.format('.02f'));

    var svgElement = '#' + elementId + ' svg';
    d3.select(svgElement)
         .datum(data)
        .transition().duration(500)
         .call(chart);

    nv.utils.windowResize(function () { chart.update() });

    nv.addGraph(function () { return chart; });
    return chart;
}

function UpdateChart(chartObj, elementId, data) {
    var svgElement = '#' + elementId + ' svg';
    d3.select(svgElement)    //Select the <svg> element you want to render the chart in.   
           .datum(data)
           .transition().duration(1000)
           .call(chartObj);
}

function DrawPieChart(elementId, data) {

    var svgroot = document.createElementNS ? document.createElementNS("http://www.w3.org/2000/svg", "svg") : document.createElement("svg");
    document.getElementById(elementId).appendChild(svgroot);

    var pieChart = nv.models.pieChart()
        .x(function (d) { return d.label })
        .y(function (d) { return d.value })
        .donut(false)
        .showLabels(false);

    var svgElement = '#' + elementId + ' svg';
    d3.select(svgElement)
        .datum(data)
      .transition().duration(1000)
        .call(pieChart);

    nv.utils.windowResize(function () { pieChart.update() });

    nv.addGraph(function () { return pieChart; });

    return pieChart;
}

function DrawDonutChart(elementId, data) {

    var svgroot = document.createElementNS ? document.createElementNS("http://www.w3.org/2000/svg", "svg") : document.createElement("svg");
    document.getElementById(elementId).appendChild(svgroot);

    var pieChart = nv.models.pieChart()
        .x(function (d) { return d.label })
        .y(function (d) { return d.value })
        .donut(true)
        .showLabels(false);

    var svgElement = '#' + elementId + ' svg';
    d3.select(svgElement)
        .datum(data)
      .transition().duration(1000)
        .call(pieChart);

    nv.utils.windowResize(function () { pieChart.update() });

    nv.addGraph(function () { return pieChart; });

    return pieChart;
}

function DrawLiveChart(canvasId) {

    var liveChart = new SmoothieChart({
        interpolation: 'linear',
        millisPerPixel: 100,
        minValue: 0,
        labels: { fillStyle: '#000', fontSize: 14 },
        grid: { millisPerLine: 10000, fillStyle: '#FFF', strokeStyle: '#d3d3d3', verticalSections: 5, sharpLines: true }, timestampFormatter: SmoothieChart.timeFormatter
    });

    liveChart.streamTo(document.getElementById(canvasId), 1000);

    return liveChart;
}

function DrawAreaChart(elementId, data) {

    var svgroot = document.createElementNS ? document.createElementNS("http://www.w3.org/2000/svg", "svg") : document.createElement("svg");
    document.getElementById(elementId).appendChild(svgroot);

    var areaChart = nv.models.stackedAreaChart()
                      .x(function (d) { return d[0] })
                      .y(function (d) { return d[1] })
                      .clipEdge(true)
                      .useInteractiveGuideline(true);
    areaChart.xAxis
            .showMaxMin(false)
            .tickFormat(function (d) { return d3.time.format('%x')(new Date(d)) });

    areaChart.yAxis
        .tickFormat(d3.format(',.2f'));

    areaChart.showControls(false);

    var svgElement = '#' + elementId + ' svg';
    d3.select(svgElement)
      .datum(data)
        .transition().duration(1000).call(areaChart);

    nv.utils.windowResize(areaChart.update);

    nv.addGraph(function () { return areaChart; });

    return areaChart;
}

function DrawBarChart(elementId, data) {

    var svgroot = document.createElementNS ? document.createElementNS("http://www.w3.org/2000/svg", "svg") : document.createElement("svg");
    document.getElementById(elementId).appendChild(svgroot);

    var barChart = nv.models.multiBarChart()
                .x(function (d) { return d[0] })
                .y(function (d) { return d[1] })
    ;

    barChart.xAxis
        .tickFormat(function (d) { return d3.time.format('%x')(new Date(d)) });

    barChart.yAxis
        .tickFormat(d3.format(',.2f'));

    barChart.multibar.stacked(true); // default to stacked
    barChart.showControls(false); // don't show controls

    var svgElement = '#' + elementId + ' svg';
    d3.select(svgElement)
        .datum(data)
        .transition().duration(1000)
        .call(barChart)
    ;

    nv.utils.windowResize(barChart.update);

    nv.addGraph(function () { return barChart; });

    return barChart;
}

function DrawBubbleChart(elementId, data) {

    var svgroot = document.createElementNS ? document.createElementNS("http://www.w3.org/2000/svg", "svg") : document.createElement("svg");
    document.getElementById(elementId).appendChild(svgroot);

    var bubbleChart = nv.models.scatterChart()
                  .showDistX(true)
                  .showDistY(true)
                  .transitionDuration(1000)
                  .color(d3.scale.category10().range());

    //Configure how the tooltip looks.
    bubbleChart.tooltipContent(function (key) {
        return '<h3>' + key + '</h3>';
    });

    //Axis settings
    bubbleChart.xAxis.tickFormat(d3.format('.02f'));
    bubbleChart.yAxis.tickFormat(d3.format('.02f'));

    //We dont want to show shapes other than circles.
    bubbleChart.scatter.onlyCircles(true);

    var svgElement = '#' + elementId + ' svg';
    d3.select(svgElement)
        .datum(data)
        .call(bubbleChart);

    nv.utils.windowResize(bubbleChart.update);

    nv.addGraph(function () { return bubbleChart; });

    return bubbleChart;
}

