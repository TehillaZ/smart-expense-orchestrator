#!/usr/bin/env node

import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const trackerPath = join(__dirname, "..", "progress-tracker.md");
const barWidth = 24;
const verbose = process.argv.includes("--verbose") || process.argv.includes("-v");

function loadProgressData() {
  const markdown = readFileSync(trackerPath, "utf8");
  const match = markdown.match(/```progress-data\s*([\s\S]*?)```/);

  if (!match) {
    throw new Error("Could not find a ```progress-data block in progress-tracker.md");
  }

  return JSON.parse(match[1].trim());
}

function renderBar(progress) {
  const filled = Math.round((progress / 100) * barWidth);
  return `${"█".repeat(filled)}${"░".repeat(barWidth - filled)}`;
}

function printChart(data) {
  const latestProgress = data.days.at(-1)?.progress ?? 0;

  console.log("");
  console.log(`${data.project} — Build Progress`);
  console.log("=".repeat(72));
  console.log(`${data.repository}`);
  console.log(`${data.summary}`);
  console.log("");

  for (const day of data.days) {
    const bar = renderBar(day.progress);
    const progressLabel = `${String(day.progress).padStart(3, " ")}%`;
    const dayLabel = `Day ${String(day.day).padStart(2, " ")}`;
    console.log(
      `${dayLabel}  ${bar}  ${progressLabel}  ${day.title}`,
    );
  }

  console.log("");
  console.log(`Overall  ${renderBar(latestProgress)}  ${String(latestProgress).padStart(3, " ")}%`);
  console.log("=".repeat(72));
  console.log("");
}

function printVerbose(data) {
  for (const day of data.days) {
    console.log(`Day ${day.day} (${day.progress}%)`);
    console.log(`Focus: ${day.focus}`);
    console.log("Completed:");
    for (const item of day.completed) {
      console.log(`  • ${item}`);
    }
    if (day.blockers?.length) {
      console.log("Blockers:");
      for (const item of day.blockers) {
        console.log(`  • ${item}`);
      }
    }
    console.log("");
  }
}

try {
  const data = loadProgressData();
  printChart(data);

  if (verbose) {
    printVerbose(data);
  } else {
    console.log("Tip: run with --verbose to print daily details.");
    console.log("");
  }
} catch (error) {
  console.error(`Failed to render progress chart: ${error.message}`);
  process.exit(1);
}
