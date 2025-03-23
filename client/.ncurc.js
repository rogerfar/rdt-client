const minorOnly = [];
const patchOnly = [];

module.exports = {
  target: (name) => {
    if (minorOnly.indexOf(name) > -1) {
      return 'minor';
    }
    if (patchOnly.indexOf(name) > -1) {
      return 'patch';
    }
    return 'latest';
  }
};
