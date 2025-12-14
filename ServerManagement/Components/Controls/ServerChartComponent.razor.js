let charts = {};

export function createOverallChart(canvas, data) {
  if (charts.overall) {
    charts.overall.destroy();
  }

  const ctx = canvas.getContext("2d");
  charts.overall = new Chart(ctx, {
    type: "doughnut",
    data: data,
    options: {
      responsive: true,
      maintainAspectRatio: true,
      plugins: {
        legend: {
          position: "bottom",
          labels: {
            padding: 15,
            font: {
              size: 14
            }
          }
        }
      }
    }
  });

  return charts.overall;
}

export function createCitiesChart(canvas, data) {
  if (charts.cities) {
    charts.cities.destroy();
  }

  const ctx = canvas.getContext("2d");
  charts.cities = new Chart(ctx, {
    type: "bar",
    data: data,
    options: {
      responsive: true,
      maintainAspectRatio: true,
      plugins: {
        legend: {
          position: "top"
        }
      },
      scales: {
        x: {
          stacked: false,
          ticks: {
            font: {
              size: 12
            }
          }
        },
        y: {
          stacked: false,
          beginAtZero: true,
          ticks: {
            stepSize: 1
          }
        }
      }
    }
  });

  return charts.cities;
}

export function disposeCharts() {
  Object.values(charts).forEach(chart => {
    if (chart && typeof chart.destroy === "function") {
      chart.destroy();
    }
  });
  charts = {};
}
