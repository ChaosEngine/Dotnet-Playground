/*global module*/
module.exports = {
	presets: [['@babel/preset-env', { targets: { node: 'current' } }]],
	plugins: ['@babel/transform-runtime', 'babel-plugin-transform-import-meta']
};