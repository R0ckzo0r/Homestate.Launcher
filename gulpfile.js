/// <binding AfterBuild='build' />
"use strict";
const gulp = require('gulp');
var del = require('del');
const zip = require('gulp-zip');
var {restore, publish} = require('gulp-dotnet-cli');
var configuration = 'Debug';

var project = {
	src: {
		compiled: './HomeState.Launcher/bin/x64/Debug/',
	}
};

gulp.task('log', function(done){
	console.log(configuration);
	console.log(project.src.compiled);
	done();
});

gulp.task('bundle-ui' , function(){
	return gulp.src('./HomeState.Launcher.Core/ui/**/*')
                    .pipe(zip("ui.bundle"))
                    .pipe(gulp.dest(project.src.compiled + '/plugins/core/'));
})

gulp.task('copy-dll', function(){
	return gulp.src([project.src.compiled + '/plugins/core/Nancy.*'])
                    .pipe(gulp.dest(project.src.compiled));
})

gulp.task('initRelease', function(done) {
		configuration = 'Release';
		project.src.compiled = './HomeState.Launcher/bin/x64/Release/';
		done();
});
gulp.task('build', gulp.series('log','copy-dll', 'bundle-ui'));

gulp.task('release',  gulp.series('initRelease', 'log', 'copy-dll', 'bundle-ui'));
