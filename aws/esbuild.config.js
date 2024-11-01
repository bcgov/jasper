const esbuild = require("esbuild")
const glob = require("glob")
const path = require("path")

const entryPoints = glob.sync("./lambdas/*/*/*.ts")

const builds = entryPoints.map((entry) => {
  const [category, handlerName] = entry.split(path.sep).slice(-3, -1)

  // Format output file name as 'dist/[category].[handlerName]/index.js'
  const outputDir = `dist/${category}.${handlerName}`

  return esbuild.build({
    entryPoints: [entry],
    outfile: path.join(outputDir, "index.js"),
    bundle: true,
    platform: "node",
    target: "node14",
    format: "cjs",
    sourcemap: true,
    minify: true
  })
})

// Run all builds in parallel
Promise.all(builds).catch(() => process.exit(1))
